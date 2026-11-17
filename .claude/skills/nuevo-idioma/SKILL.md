---
name: nuevo-idioma
description: Añade un idioma nuevo al teclado VR (enum, selector, layout JSON, lógica de pulsación) y lista los pasos que hay que hacer en el Editor de Unity.
disable-model-invocation: true
argument-hint: <Idioma en inglés, p. ej. Hebrew>
---

# Añadir el idioma: $ARGUMENTS

Lee antes `Assets/scripts/Keyboard.cs`, `KeyboardControlator.cs` y `KeyboardButton.cs`: es donde vive todo lo que hay que tocar.

## 1. Código (lo haces tú)

1. **`KeyboardControlator.validate_languajes`**: añade el idioma con el nombre exacto de `UnityEngine.SystemLanguage` (`Hebrew`, `Greek`, `Japanese`...), porque `Start()` lo compara con `Application.systemLanguage.ToString()`.
   - El índice de la lista **tiene que coincidir** con la opción del Dropdown y con la posición en `RigidBodies_Keyboards[]` y `Canvas_Keyboards[]`. Añádelo **al final** para no desplazar los demás.
2. **`Keyboard.KeyboardType`**: añade un valor solo si el idioma necesita una lógica de inserción propia (sílabas, formas contextuales, RTL). Los alfabetos simples (latino, cirílico, griego) usan `Default`.
3. **`KeyboardButton.Start()`**:
   - Si las teclas se cargan de un JSON, añade un `if` para el tipo nuevo con `Resources.Load<TextAsset>("<Idioma>")`. El nombre es el del archivo, sin extensión.
   - Añade un `case` en el `switch` con la función que inserta el carácter. Si es `Default`, no hace falta.
4. **Layout JSON** (opcional): `Assets/Resources/<Idioma>.json`, con el esquema de `letterData`. Cada clave es **el nombre del GameObject de la tecla**:
   ```json
   { "<nombre_tecla>": { "tecla_sin_shift": "", "tecla_con_shift": "", "tecla_con_altgr": "",
       "formas": { "aislada": "", "inicial": "", "medial": "", "final": "" } } }
   ```
   No crees el `.meta`: Unity lo genera al importar (el hook bloquea editar `.meta`).
5. Si hay lógica nueva (formación de sílabas, formas contextuales), escríbela como método en `Keyboard`, igual que `Korean_Syllable_Formation`, con su propio estado y su `Reset...()`.

## 2. Editor de Unity (díselo al usuario: Claude no puede editar escenas ni prefabs)

Termina con esta lista adaptada al idioma:

- [ ] Duplicar un teclado parecido, el **rígido** y el de **canvas**, y ponerle `_Keyboardtype` en el `Keyboard`.
- [ ] Renombrar las teclas para que coincidan con las claves del JSON, o rellenar `normalForms`, `CapForms` y `alterForm` en cada `KeyboardButton`.
- [ ] Añadirlos al final de `RigidBodies_Keyboards` y `Canvas_Keyboards` en el `KeyboardControlator`.
- [ ] Añadir la opción al final del Dropdown de idiomas.
- [ ] Comprobar que el Font Asset de TextMeshPro tiene los glifos, o añadir un fallback con una fuente que los cubra. Si no, salen cuadrados.
- [ ] Escritura RTL: activar `isRightToLeftText` en el campo de texto.
- [ ] Probarlo en el visor: escribir, borrar, Shift, AltGr, mover el cursor y cambiar de idioma.

## 3. Comprobación

- El hook ya valida el JSON al guardarlo.
- Revisa que el índice del idioma coincida en los cuatro sitios (lista, Dropdown y los dos arrays).
- Avisa de que hay que compilar en Unity y probarlo en el visor: aquí no hay tests.
