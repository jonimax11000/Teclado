# Teclado árabe — diseño

Fecha: 2026-11-17 · Unity 6000.3.6f1 · Escena de prueba: `Assets/Scenes/Pruebas VR.unity`

## Intención

El proyecto es un **asset de teclado VR multilingüe, global y funcional**. «Pruebas VR» es solo la escena donde se prueba. Hay que añadir el árabe con el mismo estilo que el resto de teclados: GameObjects `Buttons/Specials`, `Normal/Numbers` y `Normal/Letters/Row N`, cada tecla un prefab con `KeyboardButton`, y la disposición física imitando un teclado árabe real.

El asset es para que **otros desarrolladores lo integren en sus proyectos**. No se sabe qué harán sus usuarios con el campo, así que tiene que comportarse como un campo de texto normal: tocarlo para colocar el cursor, texto utilizable fuera del teclado, etc.

Como es un asset, lo que se escribe tiene que servir fuera del teclado. El texto del campo es **árabe Unicode lógico** (letras base, en orden de escritura). La unión de letras y la dirección derecha-a-izquierda solo afectan a cómo se dibuja.

## Objetivos

1. Elegir «العربية» en el selector de idioma y escribir árabe, tanto con el teclado rígido como con el de canvas.
2. Distribución **Windows Arabic (102)**, la variante ISO de Arabic 101, en las 4 filas: números, 3 filas de letras, Shift y AltGr. **La tabla de Windows manda.** El `Arabic.json` actual es un borrador del usuario y solo sirve de punto de partida; donde difiera (p. ej. Shift+ذ = `~` en el borrador, shadda `ّ` en Windows), gana Windows. Referencia: la tabla KBDA2 «Arabic (102)», por ejemplo en kbdlayout.info.
3. El campo muestra el árabe unido (formas contextuales, ligadura لا) y de derecha a izquierda.
4. `field.text` contiene árabe Unicode lógico: `سلام` = `0633 0644 0627 0645`.
5. Insertar, borrar y moverse con ◀ ▶ funciona en cualquier posición, también alrededor de ligaduras.
6. **Cursor visible** en el campo, también en árabe.
6b. **Tocar el campo coloca el cursor** (rayo VR o puntero) en la posición lógica correcta, también en árabe y junto a ligaduras.
7. Los demás idiomas siguen funcionando igual.

## No objetivos

- Persa, urdu o hebreo. RTLTMPro los soporta, pero no se configuran ahora.
- Orden bidi perfecto en texto mixto. Limitación conocida de RTLTMPro: `سلام 12 ab` se muestra `12 ab سلام` en vez de `ab 12 سلام`. Se acepta.
- Tashkeel desde un modo especial. Solo lo que dé Shift en Arabic 102.
- Arreglar `"Greece"` → `"Greek"` en `validate_languajes`. Es otro cambio.

## Anti-objetivos (sería un fracaso aunque «funcione»)

- Guardar en `field.text` formas de presentación (U+FE70–FEFF) o texto invertido.
- Que un idioma existente cambie de comportamiento o de aspecto (salvo el cursor, ver más abajo).
- Meter en el asset fuentes con licencia no redistribuible. Las Segoe UI de las demos de RTLTMPro **no** se usan.

## Restricciones

- Unity 6000.3.6f1, TextMeshPro dentro de `com.unity.ugui` 2.0, `TMP_InputField`.
- El código propio vive en `Assets/scripts/` (Assembly-CSharp, sin asmdef).
- El hook del proyecto bloquea editar a mano `.unity`, `.prefab` y `.meta`. Los cambios de escena se hacen con un **script de Editor** ejecutable en batchmode (el usuario autoriza tocar la escena).
- Se verifica sin visor: tests PlayMode en batchmode. La prueba final en el visor la hace el usuario.

## Enfoques considerados

| | Enfoque | A favor | En contra |
|---|---|---|---|
| A | Motor propio de unión de letras (ampliar `FormadorDeSilabasUnity`) como preprocesador de TMP | Sin dependencias | El texto mixto y los casos raros son caros de hacer bien |
| **B (elegido)** | **RTLTMPro v4.0.0** (MIT) como `textComponent` del `TMP_InputField` | Unión, ligaduras y tashkeel ya resueltos; persa y urdu gratis en el futuro | Dependencia externa; el cursor de `TMP_InputField` no sirve en RTL |
| — | Insertar formas ya unidas en el texto (lo que hacía el prototipo) | Simple | Descartado: viola el objetivo 4 |

Lo eligió el usuario tras un spike (rama `spike/rtltmpro`, desechable). Resultados:
- 6 de 7 tests en verde con texto lógico, borrado que re-forma la letra anterior, ligadura لا e inserción en medio.
- **Hallazgo clave:** `TMP_InputField` recorta `caretPosition` según los glifos dibujados. Con la ligadura لا (2 letras → 1 glifo) el texto se corrompe: `س ل ا م` acababa como `سلما`. Con un **cursor lógico propio** en el controlador todos los casos pasan.

## Diseño

### 1. Dependencias y fuente
- `Packages/manifest.json`: `"com.nosuchstudio.rtltmpro": "https://github.com/hk1ll3r/RTLTMPro.git?path=/UPMPackage#v4.0.0"` (fijado a tag).
- Fuente **Noto Sans Arabic Regular** (OFL) con su `OFL.txt`. Se crea un TMP Font Asset dinámico y se añade a los **fallbacks globales** de `TMP Settings`, así etiquetas de teclas, dropdown y campo pueden mostrar árabe. Noto Arabic no tiene latín: el latín sigue saliendo de LiberationSans, la fuente por defecto.

### 2. Campo de texto
- El `textComponent` del `TMP_InputField` de `KeyboardControlator.field` pasa a ser **`RTLTextMeshPro`**, con `Farsi = false` y `PreserveNumbers = true` para que los dígitos que se escriben no se conviertan.
- RTLTextMeshPro solo actúa si el texto contiene RTL. Para el resto de idiomas se comporta como `TextMeshProUGUI`.

### 3. Cursor lógico en `KeyboardControlator` (todos los idiomas)
- Un `int` propio es la única fuente de verdad del cursor. `InsertChar`, `InsertWord`, `InsertSpace`, `DeleteChar`, `LeftPressed` y `RightPressed` dejan de leer y escribir `field.caretPosition`.
- **◀ ▶ siguen la dirección visual.** Si el carácter junto al cursor es RTL, ◀ avanza en el texto lógico y ▶ retrocede, como en un sistema operativo real.

### 4. Cursor visible (todos los idiomas)
- Un componente propio dibuja un cursor parpadeante en el campo a partir del cursor lógico. Hace falta porque el de `TMP_InputField` no funciona en RTL.
- Se calcula la x a partir de `textInfo.characterInfo` del `RTLTextMeshPro`, mapeando índice lógico → glifo visual (el texto mostrado está invertido y la ligadura لا ocupa un glifo para dos letras).
- Se usa también para los idiomas LTR, para que haya un solo camino de código. Es el único cambio visible en los demás idiomas, y debe quedar donde hoy estaría el cursor.
- Se desactiva el caret propio de `TMP_InputField` para que no haya dos.

### 5. Teclado árabe en escena (rígido y canvas)
Los GameObjects «لينة العربية» ya existen en `ByRigidBodies` y `ByCanvas` (índice 7 de los arrays de `KeyboardControlator`). Son copias de QWERTY ESP a medio convertir. Un script de Editor (`Assets/Editor/…`, menú y `-executeMethod`) los deja así:
- `Keyboard._Keyboardtype = Arabic`.
- Cada tecla de `Numbers` y `Letters/Row 1..3`, en el orden físico ISO actual, recibe de la distribución Arabic 102:
  - nombre del GameObject = carácter base;
  - etiqueta TMP;
  - `normalForms = [base]`, `CapForms = [shift]`, `alterForm = altgr`;
  - `special_letter = false`, `not_vowel = true`.
- `Specials`: se **eliminan** las teclas de acentos españoles (`Closed`, `Open`, `Diaeresis`, `Circumflex`/`Circunflex`, `Virgulilla`/`virgulilla`). Se mantienen `Caps` (Shift), `ALT`, `Space`, `Dictated`, `Left`, `Right` y `Delete`. Los huecos que dejan se aceptan o se recolocan a criterio del builder, siempre como un teclado real.
- Teclas que insertan dos caracteres (لا, لأ, لإ, لآ) insertan la cadena completa.
- El script es **idempotente**: se puede ejecutar varias veces sin duplicar nada.

### 6. Datos de la distribución
- `Assets/Resources/Arabic.json` pasa a ser **solo entrada del script de Editor**, movido a una carpeta `Editor`. Pasa a tener las 47 teclas con `tecla_sin_shift`, `tecla_con_shift` y `tecla_con_altgr`. Se elimina `formas`: la unión de letras la hace RTLTMPro.
- Se elimina la carga del JSON en runtime de `KeyboardButton.Start()`, que hoy está por error dentro del `if Korean`. Los valores quedan horneados en escena, como en los demás teclados.

### 7. Código que se arregla o elimina
- `KeyboardButton.Start()`: el caso `Arabic` llama a `keyboard.insertChar` (hoy llama a la formación de sílabas coreana). Corea vuelve a su comportamiento sin carga de JSON.
- `KeyboardControlator.Start()`: se quita `ChangeLanguage(7)` fijo y se vuelve a `ChangeLanguage(_languaje_selector.value)`.
- Dropdown: se añade la opción «العربية» en el índice 7. Su etiqueta necesita mostrarse unida; el builder decide si con `RTLTextMeshPro` en caption e item o con la cadena ya mostrada correctamente.
- Se elimina `FormadorDeSilabasUnity.cs` y su componente en la escena (prototipo sustituido).
- Se elimina la dependencia de Newtonsoft en runtime si deja de usarse. El script de Editor puede usar `JsonUtility` o Newtonsoft.

## Criterios de aceptación (verificables en batchmode)

1. Tests PlayMode de `KeyboardControlator` con `RTLTextMeshPro`. Pasan los casos del spike: lógico, borrado, لا, inserción en medio, borrar tras ligadura y moverse.
2. Test de ◀ ▶ en texto árabe (sentido visual) y en texto latino (sin cambios).
2b. Test de tocar el campo: en `سلم`, tocar sobre el glifo de `ل` deja el cursor lógico en 1 o 2, según la mitad del glifo. En `سلام`, tocar sobre la ligadura لا (letras 1–2) lo deja en 1 o 3, nunca en 2, que estaría dentro de la ligadura.
3. Test del cursor visible: para `سلام` con cursor al final, la x del cursor queda a la izquierda del último glifo visual. Con cursor en 0, a la derecha del todo. En latín, igual que hoy.
4. Tras ejecutar el script de Editor, los dos teclados árabes tienen 0 teclas con letras latinas en `normalForms` y `_Keyboardtype = Arabic`. Ejecutarlo dos veces da la misma escena.
5. El proyecto compila sin errores y los tests existentes (si los hay) siguen en verde.
6. Prueba manual del usuario en el visor: elegir árabe en el dropdown, escribir `السلام عليكم`, borrar, mover el cursor y cambiar a otro idioma.

## Decisiones tomadas tras la revisión (calls de Claude, corregibles)

- **Dígitos:** la fila de números escribe dígitos latinos `0-9` sin modificador, como Windows Arabic 102. `PreserveNumbers = true` para que se muestren tal cual.
- **Teclas de varios caracteres (لا, لأ, لإ, لآ):** hoy `KeyboardButton.Start()` solo conecta el click si la etiqueta mide 1 carácter. Se cambia la condición a «la tecla tiene `normalForms[0]` no vacío y no es `special_letter`». Antes, el builder comprueba en escena que ningún otro teclado tiene teclas de letra con etiqueta de más de un carácter, para que su comportamiento no cambie.
- **Dirección del cursor y de ◀ ▶:** la decide el párrafo entero. Si `TextUtils.IsRTLInput(field.text)` es cierto, el párrafo es RTL y ◀ avanza en el texto lógico. Si no, comportamiento de hoy. Con el campo vacío y el teclado árabe activo, el cursor se dibuja en el borde derecho. El texto mixto sigue siendo un no-objetivo.
- **Tocar el campo:** un manejador de puntero (`IPointerClickHandler`, funciona con el rayo de XRI sobre UI) convierte el punto tocado en el glifo visual más cercano con `TMP_TextUtilities` y lo traduce a índice lógico con el mismo mapa que usa el cursor visible. Si se toca a la izquierda o a la derecha del texto, el cursor va al extremo correspondiente según la dirección del párrafo.
- **Cambios de texto externos** (limpiar el campo, dictado futuro): si `field.text` cambia sin pasar por el controlador, el cursor lógico va al final del texto (`onValueChanged`).
- **Cursor visible:** un `Image` hijo del viewport del campo, del color del texto, 2 px de ancho y alto de línea, que parpadea con `blinkSpeed`. El campo es de una sola línea (`m_LineType: 0`), así que basta con la x.
- **Dropdown:** el script de Editor añade «العربية» en el índice 7 de forma idempotente. Caption e item usan `RTLTextMeshPro`.
- **Teclas de acentos eliminadas:** se borran con sus propios eventos. El builder comprueba con el inspector de escena que nada más las referencia.
- **Tests:** asmdef para `Assets/scripts`. Ningún script usa OVR. `using Unity.VisualScripting` en `KeyboardControlator` no se usa y se quita. Los tests prueban la clase real, no una copia.
- **Idioma por defecto:** `ChangeLanguage(_languaje_selector.value)` con la lógica de `Application.systemLanguage` que ya existe, sin cambios.

## Queda a criterio del builder

- Cómo hacer testeable `KeyboardControlator` (asmdef para `Assets/scripts` u otra vía). Los GUIDs de scripts no cambian, así que la escena no se rompe.
- Cómo mapear índice lógico → glifo para el cursor.
- Recolocar los huecos que dejan las teclas de acentos eliminadas.
- Eliminar la rama del spike al terminar.
