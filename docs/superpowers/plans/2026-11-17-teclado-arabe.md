# Teclado árabe — plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Escribir árabe real (Unicode lógico) con el teclado VR, con disposición Windows Arabic (102), letras unidas en pantalla, cursor visible y colocable tocando el campo.

**Architecture:** RTLTMPro v4.0.0 dibuja el texto del `TMP_InputField` (componente `RTLTextMeshPro`). `KeyboardControlator` pasa a ser dueño de un cursor lógico propio, y un componente propio dibuja el cursor y traduce toques usando un mapa lógico↔glifo. Un script de Editor idempotente configura en escena los dos teclados árabes a partir de un JSON con la tabla de Windows.

**Tech Stack:** Unity 6000.3.6f1, uGUI 2.0 (TextMeshPro), XRI 3.3.1, RTLTMPro 4.0.0, Unity Test Framework 1.6 (PlayMode + EditMode).

**Spec:** `docs/superpowers/specs/2026-11-17-teclado-arabe-design.md`

## Global Constraints

- RTLTMPro fijado: `"com.nosuchstudio.rtltmpro": "https://github.com/hk1ll3r/RTLTMPro.git?path=/UPMPackage#v4.0.0"`.
- `field.text` siempre es árabe Unicode lógico: nunca formas U+FE70–FEFF ni texto invertido.
- `RTLTextMeshPro`: `Farsi = false`, `PreserveNumbers = true`.
- Fuente árabe: **Noto Sans Arabic Regular** (OFL) + `OFL.txt`. Nunca las Segoe UI de las demos de RTLTMPro.
- Los demás idiomas no cambian de comportamiento. El único cambio visible permitido es el cursor propio.
- No editar a mano `.unity`, `.prefab` ni `.meta` (lo bloquea el hook). La escena se cambia solo con el script de Editor.
- Commits terminan con `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

**Cómo ejecutar Unity (Unity debe estar cerrado):**
```bash
U=~/Unity/Hub/Editor/6000.3.6f1/Editor/Unity; P=/home/jonimax11000/Desktop/Teclado; O=<scratchpad>
# compilar
$U -batchmode -nographics -projectPath $P -logFile $O/compile.log -quit; grep "error CS" $O/compile.log
# tests (Platform = PlayMode | EditMode); filtro opcional: -testFilter <Clase>
$U -batchmode -nographics -projectPath $P -runTests -testPlatform PlayMode -testResults $O/r.xml -logFile $O/t.log
grep -oE '<test-run [^>]*' $O/r.xml | grep -oE '(total|passed|failed)="[0-9]+"'
```

## Review Focus

1. **Borrar o moverse en los extremos** (cursor 0 con Borrar o ◀/▶, cursor al final con ▶/◀): no cambia nada ni lanza excepción. → test en Task 2.
2. **Cambiar de idioma con texto árabe ya escrito y seguir escribiendo en latín:** el texto lógico queda `árabe+latín` y el cursor al final. → test en Task 2.
3. **Teclas que insertan dos code points** (AltGr+ض = shadda+fatha, `لا`): Borrar quita un code point cada vez, como en un sistema operativo real. → test en Task 2.
4. **El desarrollador integrador cambia `field.text` por código** (lo vacía o lo rellena): el cursor lógico va al final y la siguiente tecla se inserta ahí. → test en Task 2.
5. **Tocar el campo vacío o fuera del texto** (a la izquierda o a la derecha del texto): el cursor va al extremo correcto según la dirección y nunca queda fuera de rango. → test en Task 4.

---

### Task 1: Dependencias, asmdefs y fuente

**Files:**
- Modify: `Packages/manifest.json` (añadir RTLTMPro)
- Create: `Assets/scripts/Teclado.Runtime.asmdef`
- Modify: `Assets/scripts/KeyboardControlator.cs:6` (quitar `using Unity.VisualScripting;`)
- Create: `Assets/Fonts/NotoSansArabic/NotoSansArabic-Regular.ttf` (de `https://cdn.jsdelivr.net/gh/notofonts/notofonts.github.io/fonts/NotoSansArabic/hinted/ttf/NotoSansArabic-Regular.ttf`), `Assets/Fonts/NotoSansArabic/OFL.txt` (de `https://raw.githubusercontent.com/notofonts/arabic/main/OFL.txt`)
- Create: `Assets/Tests/PlayMode/Teclado.Tests.PlayMode.asmdef`, `Assets/Tests/PlayMode/SmokeTests.cs`

**Interfaces:**
- Produces: asmdef `Teclado.Runtime` (refs `Unity.TextMeshPro`, `UnityEngine.UI`, `RTLTMPro`; `autoReferenced: true`). Test asmdef `Teclado.Tests.PlayMode` (refs `Teclado.Runtime`, `RTLTMPro`, `Unity.TextMeshPro`, `UnityEngine.UI`, `UnityEngine.TestRunner`; `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll"]`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`).
- Si `KeyboardButton.cs` no compila por Newtonsoft dentro de la asmdef, añade `"Newtonsoft.Json.dll"` a `precompiledReferences` de `Teclado.Runtime` (con `overrideReferences: true`). Se quitará en Task 5.

- [ ] **Step 1:** `SmokeTests.CompilaYRtlDisponible`: crea un GameObject con `RTLTextMeshPro`, asigna `text = "سل"` y verifica `Assert.AreNotEqual("سل", rtl.text)` (el texto mostrado está unido).
- [ ] **Step 2:** Compila. Esperado: 0 `error CS`. Los GUID de los scripts no cambian, así que la escena sigue enlazada.
- [ ] **Step 3:** Ejecuta los tests PlayMode. Esperado: `total="1" passed="1"`.
- [ ] **Step 4:** Commit: `Añadir RTLTMPro, asmdef Teclado.Runtime y fuente Noto Sans Arabic`.

### Task 2: Cursor lógico en `KeyboardControlator`

**Files:**
- Modify: `Assets/scripts/KeyboardControlator.cs`
- Create: `Assets/scripts/CaretMapper.cs` (solo `IsInsideLigature` en esta task)
- Test: `Assets/Tests/PlayMode/KeyboardControlatorTests.cs`

**Interfaces:**
- Produces en `KeyboardControlator`:
  - `public int Caret { get; }` — índice lógico, `0..field.text.Length`
  - `public void SetCaret(int logicalIndex)` — recorta al rango y salta fuera de ligadura (hacia delante)
  - `public bool ActiveKeyboardIsRtl { get; }` — `true` si el `Keyboard` de `RigidBodies_Keyboards[index]` activo tiene `_Keyboardtype == Keyboard.KeyboardType.Arabic`; se actualiza en `ChangeLanguage`
  - `public event System.Action CaretChanged`
- Produces en `CaretMapper` (clase `public static`): `public static bool IsInsideLigature(string logical, int index)` — `true` si `logical[index-1] == 'ل'` y `logical[index]` es `ا`, `أ`, `إ` o `آ`.

- [ ] **Step 1: Tests.** Fixture: `KeyboardControlator` en un GameObject con `field` = `TMP_InputField` creado con `TMP_DefaultControls.CreateInputField`, cuyo `textComponent` se sustituye por `RTLTextMeshPro` (`Farsi=false`, `PreserveNumbers=true`). `Languaje_selector_Dropdown` y los arrays quedan nulos o vacíos.
  - `Escribir_SeGuardaLogico`: InsertChar `س`,`ل`,`ا`,`م` → `field.text == "سلام"`, `Caret == 4`
  - `LigaduraNoCorrompe`: lo anterior + `DeleteChar` ×2 → `"سل"`; `LeftPressed` (RTL ⇒ avanza en lógico; está al final ⇒ no cambia) ; `RightPressed` (RTL ⇒ retrocede) → `Caret == 1`; InsertChar `ي` → `"سيل"`
  - `FlechasEnLatin_SinCambios`: `"ab"`, `LeftPressed` → `Caret == 1`
  - `SaltaLigadura`: `"سلا"`, `SetCaret(2)` → `Caret == 3`; con `Caret==3` y `RightPressed` (RTL) → `Caret == 1`
  - `Extremos`: campo vacío: `DeleteChar`, `LeftPressed`, `RightPressed` → `""`, `Caret == 0`, sin excepción
  - `DosCodePoints_BorraUnoCadaVez`: InsertChar `"َّ"` → `Caret == 2`; `DeleteChar` → `field.text == "ّ"`
  - `CambioExterno_CursorAlFinal`: `"سل"`, `SetCaret(0)`, `field.text = "abc"` → `Caret == 3`; InsertChar `d` → `"abcd"`
  - `MixtoTrasCambiarIdioma`: InsertChar `س`,`ل`, luego `a` → `"سلa"`, `Caret == 3`
- [ ] **Step 2:** Ejecuta `-testFilter KeyboardControlatorTests`. Esperado: FAIL (no existen `Caret` ni `SetCaret`).
- [ ] **Step 3: Implementa.**
  - `InsertChar`/`InsertWord`/`InsertSpace`/`IsertLinerBreak`/`DeleteChar` operan sobre `Caret`. Nunca leen ni escriben `field.caretPosition`. `InsertWord` e `InsertSpace` delegan en `InsertChar`.
  - `LeftPressed`/`RightPressed`: si `RTLTMPro.TextUtils.IsRTLInput(field.text)`, se intercambian. Mover ±1 y, si cae dentro de ligadura, otro paso en el mismo sentido.
  - `field.onValueChanged`: si el cambio no viene del controlador (bandera interna mientras se asigna `field.text`), `Caret = field.text.Length`.
  - `Start()`: sale pronto si `Languaje_selector_Dropdown == null`. Cambia `ChangeLanguage(7)` por `ChangeLanguage(_languaje_selector.value)`.
  - `CaretChanged` se lanza cada vez que cambia `Caret` o el texto.
- [ ] **Step 4:** Ejecuta los tests. Esperado: todos PASS (los de Task 1 incluidos).
- [ ] **Step 5:** Commit: `Cursor lógico propio en KeyboardControlator`.

### Task 3: Mapa lógico↔glifo y cursor visible

**Files:**
- Modify: `Assets/scripts/CaretMapper.cs`
- Create: `Assets/scripts/CaretView.cs`
- Test: `Assets/Tests/PlayMode/CaretViewTests.cs`

**Interfaces:**
- Consumes: `KeyboardControlator.Caret`, `CaretChanged`, `ActiveKeyboardIsRtl`, `CaretMapper.IsInsideLigature`.
- Produces en `CaretMapper`:
  - `public static float CaretX(TMP_Text shown, string logical, int caret, bool rtlWhenEmpty)` — x en espacio local de `shown.rectTransform`
  - `public static int LogicalIndexAt(TMP_Text shown, string logical, Vector3 localPoint, bool rtlWhenEmpty)` — inverso; nunca devuelve un índice dentro de ligadura
- Produces: `public class CaretView : MonoBehaviour` con `public KeyboardControlator controlator; public RectTransform caret;` (un `Image` hijo del viewport, 2 px de ancho, alto de línea, color = color del texto, parpadeo con `controlator.blinkSpeed`).

- [ ] **Step 1: Tests** (fixture de Task 2 más `CaretView`; `rtl.ForceMeshUpdate()` antes de medir):
  - `Arabe_CursorAlFinal_AIzquierdaDelUltimoGlifoVisual`: `"سلام"`, `Caret 4` → `CaretX <= min(x de los glifos visibles)`
  - `Arabe_CursorAlInicio_ADerecha`: `Caret 0` → `CaretX >= max(x + ancho de los glifos)`
  - `Arabe_CursorTrasLigadura`: `"سلام"`, `Caret 3` → x entre el glifo de `م` y el de `ﻼ`
  - `Latin_IgualQueHoy`: `"abc"`, `Caret 1` → `CaretX == characterInfo[0].xAdvance` (±0.5)
  - `Vacio_TecladoArabe_BordeDerecho`: `""`, `rtlWhenEmpty=true` → x en el borde derecho del rect de texto (±1)
  - `CaretView_SigueAlControlador`: tras `InsertChar("س")` y un frame, `caret.anchoredPosition.x` coincide con `CaretX`
- [ ] **Step 2:** Ejecuta `-testFilter CaretViewTests`. Esperado: FAIL.
- [ ] **Step 3: Implementa `CaretX`.** Si el texto no es RTL (`IsRTLInput`) usa `characterInfo` directamente. Si es RTL:
  - El número de glifos visibles a la derecha del cursor es la longitud del texto que muestra RTLTMPro para el prefijo lógico `logical[..caret]`. Calcúlalo con un `RTLTextMeshPro` auxiliar o con `RTLSupport.FixRTL`, quitando `​`.
  - Recorre `textInfo.characterInfo` (solo `isVisible`) ordenado por x descendente. El cursor queda en el borde izquierdo del glifo n.º k (k = ese número), o en el borde derecho del primero si k = 0.
  - Verifica con los tests el orden de `characterInfo` cuando `isRightToLeftText = true`: es el único punto empírico.
- [ ] **Step 4: Implementa `CaretView`.**
  - `LateUpdate` coloca el cursor y gestiona el parpadeo.
  - Desactiva el cursor de `TMP_InputField` (`customCaretColor = true`, `caretColor` con alfa 0).
  - Cuando el texto es RTL, o está vacío con `ActiveKeyboardIsRtl`, alinea `textComponent` a la derecha. Si no, restaura la alineación original.
- [ ] **Step 5:** Ejecuta los tests. Esperado: todos PASS.
- [ ] **Step 6:** Commit: `Cursor visible propio con soporte RTL`.

### Task 4: Tocar el campo coloca el cursor

**Files:**
- Create: `Assets/scripts/FieldPointer.cs`
- Test: `Assets/Tests/PlayMode/FieldPointerTests.cs`

**Interfaces:**
- Consumes: `CaretMapper.LogicalIndexAt`, `KeyboardControlator.SetCaret`, `ActiveKeyboardIsRtl`.
- Produces: `public class FieldPointer : MonoBehaviour, IPointerClickHandler` con `public KeyboardControlator controlator;`. En `OnPointerClick` convierte la posición a local con `RectTransformUtility.ScreenPointToLocalPointInRectangle` (cámara = `eventData.pressEventCamera`), y llama a `controlator.SetCaret(CaretMapper.LogicalIndexAt(...))`.

- [ ] **Step 1: Tests** (llama directamente a `LogicalIndexAt` con puntos locales calculados desde `characterInfo`, y una vez a `OnPointerClick` con un `PointerEventData` construido a mano):
  - `TocarMitadIzquierdaDeLam_EnSalm`: `"سلم"`, centro-izquierda del glifo de `ل` → `2`; centro-derecha → `1`
  - `TocarLigadura_NuncaDentro`: `"سلام"`, cualquier punto sobre `ﻼ` → `1` o `3`
  - `TocarFueraDelTexto`: `"سلام"`, punto a la izquierda de todo → `4`; a la derecha de todo → `0`. Con `"abc"`: izquierda → `0`, derecha → `3`
  - `TocarCampoVacio`: `""` → `0`
- [ ] **Step 2:** Ejecuta los tests. Esperado: FAIL.
- [ ] **Step 3:** Implementa `LogicalIndexAt`: glifo visual más cercano por x, mitad del glifo, número de glifos a su derecha, y de ahí el índice lógico cuyo prefijo da ese número de glifos. Implementa también `FieldPointer`.
- [ ] **Step 4:** Ejecuta los tests. Esperado: todos PASS.
- [ ] **Step 5:** Commit: `Tocar el campo coloca el cursor lógico`.

### Task 5: `KeyboardButton`: árabe, teclas de varios caracteres y fuera el JSON en runtime

**Files:**
- Modify: `Assets/scripts/KeyboardButton.cs` (borrar el bloque `if (Korean) { Resources.Load("Arabic") ... }`, las clases `letterData` y `Forms` y `using Newtonsoft.Json;`; cambiar la condición de conexión y el `case Arabic`)
- Modify: `Assets/scripts/Teclado.Runtime.asmdef` (quitar Newtonsoft si se añadió)
- Test: `Assets/Tests/PlayMode/KeyboardButtonTests.cs`

**Interfaces:**
- Consumes: `Keyboard.insertChar(string)`, `CanvasButton.onRelease`.
- Condición de conexión nueva, exacta: `text != null && (text.text.Length == 1 || (!special_letter && normalForms.Length > 0 && normalForms[0].Length > 1))`. Comprobado: ninguna tecla de los demás teclados tiene `normalForms[0]` de más de un carácter, así que no cambian.
- `case Keyboard.KeyboardType.Arabic`: igual que `Default` (`keyboard.insertChar(text.text)`).

- [ ] **Step 1: Tests.** Jerarquía mínima: raíz con `Keyboard` (`controlator` = fixture de Task 2, `_Keyboardtype` según el test) → hijo con `KeyboardButton` → nieto con `CanvasButton` y `TextMeshProUGUI`. Esperar un frame para que corra `Start`; `canvasButton.onRelease.Invoke()`.
  - `Arabe_TeclaLamAlef_InsertaDosCodePoints`: `normalForms=["لا"]`, `CapForms=["لآ"]`, `alterForm=""` → `field.text == "لا"`
  - `Arabe_Shift_UsaCapForms`: `keyboard.CapsPressed()` y pulsar → `"لآ"`
  - `Arabe_LetraSimple`: `["ض"]` → `"ض"`
  - `Especial_DeVariosCaracteres_NoSeConecta`: `special_letter=true`, etiqueta `"Space"` → `field.text == ""`
  - `Default_LetraSimple_SinCambios`: `Default`, `["a"]` → `"a"`
- [ ] **Step 2:** Ejecuta los tests. Esperado: FAIL en los de árabe.
- [ ] **Step 3:** Implementa los cambios listados en Files.
- [ ] **Step 4:** Ejecuta todos los tests PlayMode. Esperado: PASS. Compila sin `error CS`.
- [ ] **Step 5:** Commit: `KeyboardButton: árabe, teclas multi-carácter y sin JSON en runtime`.

### Task 6: Tabla Arabic 102 y script de Editor que configura la escena

**Files:**
- Create: `Assets/Editor/Teclado.Editor.asmdef` (refs `Teclado.Runtime`, `RTLTMPro`, `Unity.TextMeshPro`, `UnityEngine.UI`; `includePlatforms: ["Editor"]`)
- Create: `Assets/Editor/Layouts/Arabic102.json`
- Create: `Assets/Editor/ArabicKeyboardSetup.cs`
- Create: `Assets/Tests/EditMode/Teclado.Tests.EditMode.asmdef` (refs `Teclado.Editor`, `Teclado.Runtime`, `RTLTMPro`, `Unity.TextMeshPro`, `UnityEngine.UI`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `includePlatforms: ["Editor"]`; nunit; `UNITY_INCLUDE_TESTS`)
- Test: `Assets/Tests/EditMode/ArabicKeyboardSetupTests.cs`
- Delete (con `git rm`, en el Step 6): `Assets/Resources/Arabic.json` (+ `.meta`), `Assets/scripts/FormadorDeSilabasUnity.cs` (+ `.meta`)

**Interfaces:**
- Produces: `public static class ArabicKeyboardSetup` con:
  - `[MenuItem("Teclado/Configurar teclado árabe")] public static void Run()` — abre `Assets/Scenes/Pruebas VR.unity`, llama a `Apply` y guarda
  - `public static void Apply(Scene scene)` — no guarda; los tests la usan
- `Arabic102.json`: `{ "rows": { "Numbers": [...13], "Row 1": [...12], "Row 2": [...12], "Row 3": [...11] } }`. Cada elemento es `{ "base": "", "shift": "", "altgr": "" }` en orden físico de izquierda a derecha, que coincide con el orden de los hijos en escena. Datos (fuente: KLC de KBDA2, kbdlayout.info; `—` = sin carácter, en el JSON `""`):

| Fila | base / shift / altgr, en orden |
|---|---|
| Numbers | `>`/`<`/— · `1`/`!`/— · `2`/`@`/— · `3`/`#`/— · `4`/`$`/`¤` · `5`/`%`/— · `6`/`^`/— · `7`/`&`/— · `8`/`*`/— · `9`/`)`/— · `0`/`(`/— · `-`/`_`/— · `=`/`+`/— |
| Row 1 | `ض`/U+064E/U+0651 U+064E · `ص`/U+064B/U+0651 U+064B · `ث`/U+064F/U+0651 U+064F · `ق`/U+064C/U+0651 U+064C · `ف`/`لإ`/— · `غ`/`إ`/— · `ع`/U+2018/— · `ه`/`÷`/— · `خ`/`×`/— · `ح`/`؛`/— · `ج`/`}`/— · `د`/`{`/— |
| Row 2 | `ش`/`\`/— · `س`/—/— · `ي`/`]`/— · `ب`/`[`/— · `ل`/`لأ`/— · `ا`/`أ`/— · `ت`/`ـ`/— · `ن`/`،`/— · `م`/`/`/— · `ك`/`:`/— · `ط`/`"`/— · `ذ`/U+0651/— |
| Row 3 | `ـ`/`\|`/— · `ئ`/`~`/— · `ء`/U+0652/— · `ؤ`/U+0650/U+0651 U+0650 · `ر`/U+064D/U+0651 U+064D · `لا`/`لآ`/— · `ى`/`آ`/— · `ة`/U+2019/— · `و`/`,`/— · `ز`/`.`/— · `ظ`/`؟`/— |

  Decisiones: Shift+`س` (U+0000) y AltGr+`س` (U+20C1, sin glifo en Noto) quedan vacíos.

- `Apply` hace, en este orden:
  1. Para cada raíz `Keyboards/ByRigidBodies/<árabe>` y `Keyboards/ByCanvas/<árabe>` (es el hijo cuyo `Keyboard` está en el índice 7 de `RigidBodies_Keyboards` y `Canvas_Keyboards` de `KeyboardControlator`): `_Keyboardtype = Arabic`.
  2. Para cada fila, hijo i ↔ entrada i de la tabla. Si los números no cuadran, `throw`. En cada tecla: `name = base`, etiqueta `TextMeshProUGUI` = `base`, `normalForms = [base]`, `CapForms = [shift]`, `alterForm = altgr`, `special_letter = false`, `not_vowel = true`. Usa `Undo.RecordObject` o `SerializedObject` para que se guarde en la instancia del prefab.
  3. En `Buttons/Specials` destruye los hijos cuyo nombre, en minúsculas, esté en `{closed, open, diaeresis, circumflex, circunflex, virgulilla}`. Los huecos se dejan como están.
  4. `TMP_InputField` de `KeyboardControlator.field`: sustituye el `TextMeshProUGUI` de `textComponent` por `RTLTextMeshPro` en el mismo GameObject. Copia `font`, `fontSize`, `color`, `alignment`, `margin`, `enableAutoSizing`, `raycastTarget` y `richText`; pon `Farsi=false` y `PreserveNumbers=true`; reasigna `field.textComponent`. Añade `CaretView` y `FieldPointer` si faltan, con un `Image` hijo del viewport llamado `Caret`.
  5. `TMP_Dropdown` de `Languaje_selector_Dropdown`: si no hay opción `العربية` en el índice 7, añádela. Sustituye `captionText` e `itemText` por `RTLTextMeshPro` con la misma copia de propiedades.
  6. Elimina el componente `FormadorDeSilabasUnity` (o «Missing Script» con `GameObjectUtility.RemoveMonoBehavioursWithMissingScript`) de la escena.
  7. Crea `Assets/Fonts/NotoSansArabic/NotoSansArabic SDF.asset` (dinámico, como en `Assets/Spike/Editor/RtlSpikeBuilder.cs` de la rama `spike/rtltmpro`) si no existe, y añádelo a `TMP Settings` → `m_fallbackFontAssets` si no está.
  - Cada paso comprueba antes de actuar, para que ejecutarlo dos veces no duplique nada.

- [ ] **Step 1: Tests EditMode.** `EditorSceneManager.OpenScene` de `Pruebas VR.unity` → `Apply` → asserts → en `TearDown`, reabrir la escena sin guardar.
  - `TecladosArabes_SinLatinYTipoArabe`: en ambos teclados, `_Keyboardtype == Arabic` y ningún `normalForms[0]` contiene `[A-Za-zñçÑÇ]`
  - `TeclasConcretas`: Row 3 índice 5 → `normalForms[0] == "لا"`, `CapForms[0] == "لآ"`; Row 2 índice 11 → `"ذ"` / `"ّ"`; Numbers índice 0 → `">"`
  - `SinTeclasDeAcentos`: ningún hijo de `Specials` con esos nombres
  - `CampoUsaRtl`: `field.textComponent is RTLTextMeshPro` y existen `CaretView` y `FieldPointer`
  - `DropdownTieneArabe`: `options[7].text == "العربية"` y `options.Count == 8`
  - `Idempotente`: `Apply` dos veces → mismas comprobaciones, `options.Count == 8` y un solo `Caret`
- [ ] **Step 2:** Ejecuta los tests EditMode. Esperado: FAIL.
- [ ] **Step 3:** Escribe el JSON y `ArabicKeyboardSetup`.
- [ ] **Step 4:** Ejecuta los tests EditMode. Esperado: PASS.
- [ ] **Step 5:** Aplica de verdad: `$U -batchmode -nographics -projectPath $P -executeMethod ArabicKeyboardSetup.Run -logFile $O/setup.log -quit`. Esperado: sin `Exception` en el log y `git status` mostrando cambios en `Pruebas VR.unity`.
- [ ] **Step 6:** `git rm` de `Assets/Resources/Arabic.json(.meta)` y `Assets/scripts/FormadorDeSilabasUnity.cs(.meta)`. Compila (0 `error CS`) y ejecuta todos los tests, EditMode y PlayMode. Esperado: todos PASS.
- [ ] **Step 7:** Commit: `Teclado árabe Arabic 102 configurado en escena`.

### Task 7: Verificación final y entrega

- [ ] **Step 1:** Compila. Ejecuta todos los tests EditMode y PlayMode. Pega los totales.
- [ ] **Step 2:** Comprueba `git diff main --stat`: no hay cambios en otros teclados. Opcional: el inspector de escena confirma que los `normalForms` de QWERTY US, ESP, AZERTY, QWERTZ, JCUKEN, Korean y EUCLIDES GP no han cambiado.
- [ ] **Step 3:** Pasa al usuario la checklist manual para el visor: elegir «العربية», escribir `السلام عليكم`, Shift (tashkeel), AltGr+ض, borrar, ◀ ▶, tocar el campo, cambiar a English y escribir `abc`.
- [ ] **Step 4:** Tras la aprobación del usuario: borrar la rama `spike/rtltmpro`.
