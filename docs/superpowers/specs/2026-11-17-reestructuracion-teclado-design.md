# Reestructuración del teclado: distribuciones como datos y teclas muertas reales

Fecha: 2026-11-17 · Unity 6000.3.6f1 · Rama: `feature/reestructuracion` (parte de `feature/teclado-arabe`, PR #1)

## Intención

El proyecto es un asset de teclado VR multilingüe que otros desarrolladores integran en sus proyectos. El autor (el usuario) es el único que añade idiomas, **poco a poco, para que el asset parezca vivo**. Por eso añadir un idioma tiene que ser barato: un archivo de datos y una herramienta, sin montar teclas a mano ni tocar código.

Además, cada idioma tiene que comportarse como **su teclado real de Windows**: teclas muertas (`´` + `e` = `é`), Shift y AltGr reales. Desaparecen las teclas para elegir el tipo de tilde (`Closed`, `Open`, `Diaeresis`, `Circumflex`, `Virgulilla`, `Dieresis_Accented`).

Más adelante se añadirán **chino y japonés**, que escriben con texto provisional y una barra de candidatos. El núcleo tiene que admitirlos desde ahora para no rehacerlo después.

## Objetivos

1. **Distribución como datos:** un asset `KeyboardLayout` por idioma, importado del fichero oficial de Windows `.klc` (por ejemplo de kbdlayout.info), con las capas base, Shift, AltGr y Shift+AltGr, las ligaduras y las tablas de teclas muertas.
2. **Generar teclados desde plantillas físicas:** a partir de un `KeyboardLayout` y una plantilla, una herramienta de Editor crea los teclados rígido y de canvas en la escena.
3. **Comportamiento real por idioma:**

   | Idioma (dropdown) | Distribución Windows | Plantilla | Compositor |
   |---|---|---|---|
   | English | US (KBDUS) | ANSI | Directo |
   | Español | Spanish (KBDSP) | ISO | Teclas muertas |
   | Français | French (KBDFR) | ISO | Teclas muertas |
   | Deutsch | German (KBDGR) | ISO | Teclas muertas |
   | Русский | Russian (KBDRU) | ANSI-14 | Directo |
   | 한국어 | Korean (Dubeolsik, KS X 5002) | ANSI | Hangul |
   | Ελληνικά | Greek (KBDHE, monotónico) | ANSI-14 | Teclas muertas |
   | العربية | Arabic (102) (KBDA2) | ISO | Directo |

4. **Núcleo independiente de Unity**, testeable sin escena: estado del teclado, compositores y búfer de texto.
5. **Texto provisional (preedit) y candidatos** en la interfaz del compositor. El preedit se ve subrayado en el campo. Los candidatos quedan en la interfaz, sin interfaz gráfica todavía.
6. Se conserva todo lo del árabe: RTLTMPro, cursor lógico, `CaretView`, `FieldPointer`, `CaretMapper`, `RtlShaper` y la auditoría de glifos.
7. Se cumple SOLID: cada clase tiene una responsabilidad y un idioma nuevo no modifica código existente (abierto/cerrado).

## No objetivos

- Barra de candidatos y compositores Pinyin o Romaji (chino y japonés). Solo quedan preparadas sus interfaces.
- Plantillas de otras formas: JIS, flick de 12 teclas, compactas para VR.
- Generar teclados con posiciones calculadas desde cero (opción B, descartada).
- Que los desarrolladores integradores creen sus propias distribuciones: no se documenta ni se le pone interfaz gráfica para ellos.
- Sustituir la fuente `segoeui SDF` de MRTK de los teclados de canvas. Ver Riesgos.
- Corregir los menores aplazados del árabe, salvo que caigan solos con el código nuevo.

## Anti-objetivos (fracaso aunque «funcione»)

- Que el texto que recibe el integrador contenga marcas de preedit, formas de presentación árabes o etiquetas de rich text.
- Que un idioma escriba algo distinto de lo que escribe su teclado de Windows con la misma secuencia de teclas, dentro de lo que la tabla `.klc` define.
- Que añadir un idioma requiera editar una clase existente (`switch` por idioma, flags nuevos…).
- Perder el aspecto visual actual de los teclados: las plantillas salen de los teclados hechos a mano.

## Restricciones

- Unity 6000.3.6f1, uGUI 2.0/TMP, XRI 3.3.1, RTLTMPro 4.0.0, Test Framework 1.6.
- El hook del proyecto bloquea editar `.unity`, `.prefab` y `.meta` a mano. La escena solo se cambia con scripts de Editor (en batchmode).
- Sin visor para verificar: tests EditMode/PlayMode en batchmode y prueba manual final del usuario.
- Prefabs de tecla: `Assets/Prefabs/VR Key.prefab` (rígido) y `Assets/Prefabs/Button.prefab` (canvas), 380 instancias cada uno.
- Forma actual de las filas (Numbers/Row 1/Row 2/Row 3):
  - **ISO** 13/12/12/11: QWERTY ESP, AZERTY, QWERTZ, árabe
  - **ANSI** 13/13/11/10: QWERTY US, Korean
  - **ANSI-14** 14/12/11/10: JCUKEN, EUCLIDES GP (la tecla `\` va en la fila de números)
- Teclas especiales actuales: `Caps`, `ALT`, `Space`, `Dictated`, `Left`, `Right`, `Delete`. No hay Enter.

## Enfoques considerados

| | Enfoque | A favor | En contra |
|---|---|---|---|
| **A (elegido)** | **Plantillas por forma física:** un teclado hecho a mano por forma (rígido y canvas); la herramienta lo clona y lo rellena con la distribución | Conserva el aspecto actual; cada idioma nuevo no tiene trabajo manual; ya funcionó con el árabe | Una forma física nueva (JIS, flick) requiere hacer una plantilla a mano |
| B | Generar teclados desde cero con posiciones calculadas | Cualquier forma (flick, compactos) | Hay que rehacer el aspecto visual; no aporta nada para teclados de ordenador |
| C | Seguir montando cada teclado a mano; la herramienta solo rellena datos | Nada nuevo que construir | Unas 100 teclas a mano por idioma |

Núcleo con compositores y preedit desde el principio, en vez de «tecla → carácter + teclas muertas»: lo eligió el usuario porque vendrán chino y japonés.

## Diseño

### Capas (Modelo–Vista–Presentador guiado por datos)

```
Datos     KeyboardLayout (ScriptableObject)  ←  KlcImporter (Editor)
Núcleo    KeyboardState · IComposer {Direct, DeadKey, Hangul} · TextBuffer · KeyboardSession
Vista     KeyView · KeyboardView · InputFieldView (RTL, CaretView, PreeditView, FieldPointer)
          LanguageSelector · KeyboardModeSwitcher
Editor    KlcImporter · KeyboardGenerator (plantillas) · GlyphAudit
```

El núcleo no referencia `UnityEngine` (salvo, si hace falta, tipos de datos serializables). Las vistas no saben nada de idiomas. Todo lo propio de un idioma está en su `KeyboardLayout` y en el compositor que ese layout declara.

### 1. `KeyboardLayout` (datos)

- `ScriptableObject` en `Assets/Layouts/<Idioma>.asset`. Contiene:
  - Nombre visible (para el dropdown).
  - Plantilla (`ISO`, `ANSI` o `ANSI-14`).
  - Tipo de compositor (`Direct`, `DeadKey` o `Hangul`).
  - `isRtl`.
  - Lista de teclas.
- Cada tecla se identifica por su **scancode** (el `SC` del `.klc`, p. ej. `10` = Q). Lleva:
  - El texto de cada capa: base, Shift, AltGr y Shift+AltGr. Puede ser una cadena de varios code points, por las ligaduras del KLC.
  - Si es tecla muerta en cada capa.
  - El flag de Bloq Mayús (columna `Cap` del KLC: `0`, `1` o `SGCap`).
- Tablas de teclas muertas: `deadChar → { baseChar → resultado }`.
- Para el coreano, la tabla de jamo de Dubeolsik (base y Shift) se introduce a mano en el asset, porque el KLC de Windows coreano es el de EE. UU.

### 2. `KlcImporter` (Editor)

- Menú *Teclado → Importar distribución .klc…*. Lee el `.klc` (UTF-16) y crea o actualiza el `KeyboardLayout` sin tocar los campos que no vienen del KLC (nombre visible, plantilla, compositor).
- Interpreta las secciones `SHIFTSTATE`, `LAYOUT` (incluido el sufijo `@` de tecla muerta), `LIGATURE`, `DEADKEY` y `KEYNAME_DEAD`. `-1` significa sin carácter. Los códigos de control (< U+0020) se ignoran.
- El árabe pasa al sistema nuevo importando KBDA2. Un test comprueba que coincide con `Assets/Editor/Layouts/Arabic102.json`, que después se borra.

### 3. Núcleo

- **`KeyboardState`:** Shift, AltGr y Bloq Mayús.
  - Shift y AltGr son de un solo uso: afectan a la siguiente tecla y se sueltan.
  - Doble toque en Shift activa o desactiva Bloq Mayús (como en el móvil, porque en VR no se puede mantener pulsada una tecla).
  - Bloq Mayús se combina con Shift según el flag `Cap` de cada tecla, como en Windows.
  - Lanza un evento cuando cambia, para que las vistas actualicen las etiquetas.
- **`IComposer`:**
  ```
  ComposeResult Process(KeyStroke key)   // tecla de carácter
  ComposeResult Backspace()              // dentro de una composición; null si no hay composición
  string Flush()                         // confirma lo pendiente (espacio, flechas, toque, cambio de idioma)
  void Cancel()
  IReadOnlyList<string> Candidates       // vacío salvo Pinyin/Romaji (futuro)
  ComposeResult SelectCandidate(int i)   // futuro
  ```
  `ComposeResult` lleva `commit` (texto que se inserta definitivamente) y `preedit` (texto provisional actual, posiblemente vacío).
  - **`DirectComposer`:** commit = texto de la capa.
  - **`DeadKeyComposer`** (semántica de Windows):
    - Una tecla muerta deja el preedit con el acento sin combinar.
    - Si la siguiente tecla está en la tabla, el resultado se confirma.
    - Tecla muerta + espacio: se confirma el acento sin combinar (la entrada `0020` de la tabla).
    - Tecla muerta + algo que no está en la tabla: se confirma el acento y después ese carácter.
    - Borrar cancela la tecla muerta pendiente.
  - **`HangulComposer`** (autómata Dubeolsik estándar):
    - Forma la sílaba con consonante inicial, vocal (también compuestas: ㅘ, ㅙ…) y consonante final (también dobles: ㄳ, ㄺ…).
    - Si llega una vocal tras una consonante final, la final pasa a la sílaba siguiente (`각` + `ㅏ` → `가가`). Si la final era doble, se divide (`닭` + `ㅏ` → `달가`).
    - Borrar dentro de la sílaba quita un jamo cada vez.
    - Sustituye a `Keyboard.Korean_Syllable_Formation`.
- **`TextBuffer`:** texto confirmado, cursor lógico (lo que hoy hay en `KeyboardControlator`: inserción, borrado, flechas visuales, salto de ligaduras, acotado) y el rango de preedit.
  - El texto visible es el confirmado con el preedit insertado en el cursor.
  - Expone `Text` (lo que se muestra), `CommittedText` (sin preedit) y el evento `Committed(string)` para integradores.
- **`KeyboardSession`:** une el layout activo, el estado, el compositor y el búfer.
  - Recibe teclas por scancode y las teclas especiales (Shift, AltGr, Espacio, Borrar, ◀ ▶, Dictado).
  - Al cambiar de idioma, al mover el cursor o al tocar el campo, hace `Flush()` del compositor.

### 4. Vista y Unity

- **`KeyView`** (en cada tecla generada):
  - Tiene scancode y referencia a su etiqueta.
  - Pinta el texto de la capa activa. Las teclas muertas se pintan con el acento suelto. Las marcas combinantes se pintan sobre `◌` (U+25CC). Las etiquetas árabes se pintan unidas con `RtlShaper`.
  - Al pulsar avisa a `KeyboardView`.
  - Usa `ButtonVR` (rígido) o `CanvasButton` (canvas) sin cambios. Sustituye a `KeyboardButton`.
- **`KeyboardView`** (raíz de cada teclado): tiene el `KeyboardLayout` y las teclas especiales, reenvía a `KeyboardSession` y refresca las etiquetas al cambiar el estado. Sustituye a `Keyboard`.
- **`InputFieldView`:** sincroniza `TextBuffer` con el `TMP_InputField`. `field.text` = texto visible. Reutiliza:
  - `RTLTextMeshPro`, `CaretView`, `FieldPointer` y `CaretMapper`.
  - Un nuevo **`PreeditView`**, que dibuja un subrayado bajo el rango de preedit usando `CaretMapper` (las dos x del rango).
  - Si `field.text` cambia por fuera del teclado, se cancela la composición y el búfer adopta el texto nuevo con el cursor al final, como hoy.
- **`LanguageSelector`:** dropdown e idioma del sistema. Sale de `KeyboardControlator`. Rellena las opciones a partir de la lista de layouts generados, así que un idioma nuevo aparece solo.
- **`KeyboardModeSwitcher`:** cambio entre rígido y canvas (`ChangeToCanvas`/`ChangeToRigid`).
- `KeyboardControlator` desaparece repartido entre `KeyboardSession`, `TextBuffer`, `InputFieldView`, `LanguageSelector` y `KeyboardModeSwitcher`. Las referencias de la escena se recablean con el generador.

### 5. Plantillas y `KeyboardGenerator` (Editor)

- Tres plantillas, cada una con su versión rígida y de canvas, hechas a partir de los teclados actuales:
  - **ISO** desde QWERTY ESP
  - **ANSI** desde QWERTY US
  - **ANSI-14** desde JCUKEN
- Se guardan como prefabs en `Assets/Prefabs/Templates/`. Los crea el script de Editor, no la mano.
- Cada tecla de plantilla tiene `KeyView` con su scancode, asignado una vez al crear la plantilla.
- Las teclas de tilde se eliminan de las plantillas. Se mantienen `Caps` (→ Shift), `ALT` (→ AltGr), `Space`, `Dictated`, `Left`, `Right` y `Delete`.
- Menú *Teclado → Generar teclados*. Para cada `KeyboardLayout` de `Assets/Layouts/`:
  - Instancia su plantilla en `ByRigidBodies` y `ByCanvas`, asigna el layout y nombra las teclas.
  - Recablea `LanguageSelector` y `KeyboardModeSwitcher`.
  - Es **idempotente**: regenera solo lo que cambió y no duplica.
  - Sustituye a los 16 teclados actuales y a `ArabicKeyboardSetup`, que se borra.
- Una tecla que el layout define y la plantilla no tiene (p. ej. `OEM_102` en ANSI) se omite con un aviso. Una tecla de la plantilla sin definir en el layout queda vacía y desactivada.

### 6. Añadir un idioma (flujo del autor)

1. Descargar el `.klc` y *Teclado → Importar distribución .klc…*.
2. En el asset: nombre visible, plantilla y compositor.
3. *Teclado → Generar teclados*.
4. *Teclado → Auditar glifos*. Si faltan glifos, añadir una fuente de respaldo.

Sin tocar código, salvo que haga falta un compositor nuevo (chino o japonés).

## Criterios de aceptación

1. **Importador:** para KBDSP, `´` es tecla muerta y `´`+`e` → `é`, `´`+espacio → `´`. Las ligaduras de KBDA2 (`لا`) se importan. El árabe importado coincide tecla a tecla con `Arabic102.json`.
2. **Compositores (EditMode, sin escena):**
   - Español: `´ e`→`é`, `´ t`→`´t`, `¨ u`→`ü`, `^ a`→`â`, `` ` a``→`à`, `´ Borrar`→nada.
   - Francés: `^ e`→`ê`, `¨ i`→`ï`.
   - Alemán: `^ o`→`ô`, `´ e`→`é`.
   - Griego: `΄ α`→`ά`, `¨ ι`→`ϊ`, `΅ ι`→`ΐ` (según KBDHE).
   - Coreano: `ㅎㅏㄴㄱㅡㄹ`→`한글`, `ㄱㅏㄱㅏ`→`가가`, `ㄷㅏㄹㄱㅏ`→`달가`, `ㅇㅗㅏ`→`와`; Borrar sobre `한` deja `하`.
   - Ruso, árabe e inglés: directo.
3. **Estado:** Shift de un solo uso; doble toque = Bloq Mayús; Bloq Mayús respeta el flag `Cap` (en español, Bloq Mayús + `1` sigue dando `1`).
4. **Preedit:** durante una composición, `CommittedText` no contiene el preedit, `field.text` sí, y el subrayado cubre exactamente su rango.
5. **Generador:**
   - Tras *Generar teclados*, los 8 idiomas existen en rígido y canvas con la plantilla de la tabla. No queda ninguna tecla de tilde.
   - Ejecutarlo dos veces da la misma escena.
   - El dropdown lista los 8 idiomas en el orden actual.
6. **Paridad con hoy:** para cada idioma, la capa base y la de Shift de cada tecla coinciden con lo que escribía el teclado actual. Las diferencias son intencionadas y se listan (las tildes pasan a teclas muertas, y cualquier carácter que la distribución real de Windows ponga en otra tecla).
7. **Glifos:** `GlyphAudit` recorre todas las capas y todos los resultados de teclas muertas de todos los layouts: 0 caracteres sin glifo.
8. **Regresión del árabe:** siguen en verde los 44 tests de la rama `feature/teclado-arabe`, adaptados a las clases nuevas donde cambie el nombre.
9. Prueba manual del usuario en el visor: cada idioma, teclas muertas en español, francés y griego, coreano, y cambio de idioma con composición pendiente.

## Riesgos

- **Licencia de Segoe UI:** los teclados de canvas usan `segoeui SDF` de MRTK. Para distribuir el asset conviene sustituirla por una fuente libre (Noto). Queda fuera de este proyecto y debería ser el siguiente.
- **Paridad:** los teclados actuales tenían caracteres en posiciones propias, no de Windows (p. ej. acentos en el teclado US). Pasar a Windows cambia lo que escriben algunas teclas. Es intencionado, pero el usuario debe ver la lista de diferencias (criterio 6).
- **Griego:** el usuario eligió el griego monotónico estándar. El teclado actual tenía acentos graves (`ὰ ὲ ὴ`…) que el monotónico no tiene; aparecerán en la lista de diferencias como eliminados a propósito.

## Decisiones tras la revisión (calls de Claude, corregibles)

- **Dictado:** hoy la tecla `Dictated` no llama a nada (no hay ningún evento cableado en la escena). Se mantiene como tecla visible y sin comportamiento, como punto de enganche futuro. `KeyboardSession` no la procesa.
- **Enter:** `IsertLinerBreak` no se usa en la escena y el campo es de una línea. Se elimina. `TextBuffer` no incluye salto de línea.
- **Scancodes de plantilla:** cada plantilla fija por código el orden de scancodes de cada fila, de izquierda a derecha:
  - **Numbers:** `29 02 03 04 05 06 07 08 09 0A 0B 0C 0D`, más `2B` al final en ANSI-14.
  - **Row 1:** `10 11 12 13 14 15 16 17 18 19 1A 1B`, más `2B` al final en ANSI.
  - **Row 2:** `1E 1F 20 21 22 23 24 25 26 27 28`, más `2B` al final en ISO.
  - **Row 3:** `2C 2D 2E 2F 30 31 32 33 34 35`, más `56` al principio en ISO.
  - No se deduce de los caracteres actuales.
- **Origen de los `.klc`:** el builder los descarga de kbdlayout.info durante la implementación. **No se commitean los `.klc`**, porque son ficheros derivados de Microsoft. Sí se commitean los `KeyboardLayout` generados, que son datos de distribución. Los tests del importador usan fragmentos `.klc` sintéticos mínimos; los tests de comportamiento usan los assets `KeyboardLayout`.
- **Coreano:** el builder escribe la tabla Dubeolsik desde KS X 5002 (Shift da ㄲ ㄸ ㅃ ㅆ ㅉ ㅒ ㅖ). El texto de cada capa es el jamo de compatibilidad (U+3131–U+318E), y el compositor Hangul trabaja sobre esos caracteres.
- **`KeyStroke`** = `{ scancode, text, isDead }`. Lo resuelve `KeyboardSession` con `KeyboardState` y el layout (capa activa más el flag `Cap`) antes de pasarlo al compositor. El compositor nunca ve el estado.
- **Shift/AltGr con una tecla muerta pendiente:**
  - Pulsar Shift o AltGr no confirma ni cancela la composición.
  - El modo de un solo uso se consume con la siguiente tecla de carácter.
  - Espacio, Borrar, flechas, toque en el campo y cambio de idioma también lo desactivan.
- **Orden e idioma del sistema:** `KeyboardLayout` tiene `order` (int) y `systemLanguage` (`UnityEngine.SystemLanguage`). El dropdown se ordena por `order`, y el idioma inicial se elige comparando `Application.systemLanguage`. Esto corrige de paso `"Greece"` → `Greek`.
- **Clases antiguas:** el asset aún no se ha publicado, así que se eliminan `Keyboard`, `KeyboardButton`, `KeyboardControlator` y `ArabicKeyboardSetup` sin capa de compatibilidad. El generador quita los teclados antiguos y todos sus eventos de la escena. `ResetKoreanSyllable`, cableado una vez en la escena, se sustituye por `Flush()`.
- **Plantillas:** el script crea los 3 prefabs de plantilla una sola vez a partir de los teclados vivos, y se commitean. Un solo Shift (antes `Caps`) y un solo AltGr (antes `ALT`). Bloq Mayús solo se activa con doble toque en Shift: no hay tecla Bloq Mayús.
- **`Committed(string)`:** se lanza en cada confirmación de texto (una por pulsación que confirma algo).
- **asmdef:** el núcleo va en `Teclado.Core` (sin referencias a Unity, salvo los tipos serializables que haga falta). Los tests PlayMode de `CaretView` y `FieldPointer` se adaptan a las clases nuevas.

## Queda a criterio del builder

- Representación interna exacta de `KeyboardLayout` (listas serializables) y de `ComposeResult`.
- Cómo se asignan los scancodes a las teclas de plantilla (por ejemplo, haciendo coincidir sus caracteres actuales con el KLC correspondiente).
- Cómo se migra la escena paso a paso, siempre que cada paso deje el proyecto compilando y con tests en verde.
- Nombres de asmdef (`Teclado.Core` para el núcleo sin Unity es una opción).
