using System;
using System.Collections.Generic;

using UnityEngine;


public class FormadorDeSilabasUnity  : MonoBehaviour
{
    // Tabla de formas de letras árabes (Forma: [Aislada, Final, Inicial, Media])
    static Dictionary<char, char[]> arabicLetterForms = new Dictionary<char, char[]>
    {
        { 'ا', new char[] { 'ا', 'ﺎ', 'ﺍ', 'ﺎ' } }, // Alef
        { 'ل', new char[] { 'ل', 'ﻞ', 'ﻟ', 'ﻠ' } }, // Lam
        { 'س', new char[] { 'س', 'ﺲ', 'ﺳ', 'ﺴ' } }, // Seen
        { 'م', new char[] { 'م', 'ﻢ', 'ﻣ', 'ﻤ' } }, // Meem
        { 'ع', new char[] { 'ع', 'ﻊ', 'ﻋ', 'ﻌ' } }, // Ain
        { 'ي', new char[] { 'ي', 'ﻲ', 'ﻳ', 'ﻴ' } }, // Yeh
        { 'ك', new char[] { 'ك', 'ﻚ', 'ﻛ', 'ﻜ' } }, // Kaf
        { 'آ', new char[] { 'آ', 'ﺂ', 'ﺁ', 'ﺂ' } }, // Alef Madda
        { 'أ', new char[] { 'أ', 'ﺄ', 'ﺃ', 'ﺄ' } }, // Alef Hamza Arriba
        { 'إ', new char[] { 'إ', 'ﺈ', 'ﺇ', 'ﺈ' } }, // Alef Hamza Abajo
        { 'ى', new char[] { 'ى', 'ﻰ', 'ﻯ', 'ﻰ' } }, // Alef Maqsura
        { 'ئ', new char[] { 'ئ', 'ﺊ', 'ﺋ', 'ﺌ' } }, // Yeh Hamza
    };

    // Tabla de ligaduras árabes
    static Dictionary<string, char> ligatures = new Dictionary<string, char>
    {
        { "لا", 'ﻻ' },   // Ligadura para Lam-Alef
        { "لآ", 'ﻵ' },   // Ligadura para Lam-Alef Madda
        { "لأ", 'ﻷ' },   // Ligadura para Lam-Alef Hamza arriba
        { "لإ", 'ﻹ' },   // Ligadura para Lam-Alef Hamza abajo
        { "لى", 'ﻼ' },   // Ligadura para Lam-Alef Maqsura
    };

    // Función para aplicar ligaduras cuando sea necesario
    static string ApplyLigature(string textSoFar, char newLetter)
    {
        // Verifica si el nuevo carácter junto con el anterior forman una ligadura
        if (textSoFar.Length > 0)
        {
            string lastTwoLetters = textSoFar[textSoFar.Length - 1].ToString() + newLetter;
            if (ligatures.ContainsKey(lastTwoLetters))
            {
                // Reemplaza las últimas dos letras con la ligadura
                return textSoFar.Substring(0, textSoFar.Length - 1) + ligatures[lastTwoLetters];
            }
        }
        return textSoFar + newLetter;
    }

    // Función para obtener la forma correcta de una letra árabe
    static char GetCorrectForm(char currentLetter, char? previousLetter, char? nextLetter)
    {
        bool hasPrevious = previousLetter.HasValue && arabicLetterForms.ContainsKey(previousLetter.Value);
        bool hasNext = nextLetter.HasValue && arabicLetterForms.ContainsKey(nextLetter.Value);

        if (!hasPrevious && !hasNext)
            return arabicLetterForms[currentLetter][0]; // Aislada
        if (!hasPrevious && hasNext)
            return arabicLetterForms[currentLetter][2]; // Inicial
        if (hasPrevious && !hasNext)
            return arabicLetterForms[currentLetter][1]; // Final
        return arabicLetterForms[currentLetter][3]; // Media
    }

    // Función para procesar letra por letra
    static string ProcessLetterByLetter(string textSoFar, char newLetter)
    {
        // Paso 1: Agregar la nueva letra y aplicar ligaduras si corresponde
        textSoFar = ApplyLigature(textSoFar, newLetter);

        // Paso 2: Procesar la forma de cada letra en la cadena
        char[] processedText = new char[textSoFar.Length];
        for (int i = 0; i < textSoFar.Length; i++)
        {
            char currentLetter = textSoFar[i];
            char? previousLetter = i > 0 ? textSoFar[i - 1] : (char?)null;
            char? nextLetter = i < textSoFar.Length - 1 ? textSoFar[i + 1] : (char?)null;

            // Solo procesar letras árabes
            if (arabicLetterForms.ContainsKey(currentLetter))
            {
                processedText[i] = GetCorrectForm(currentLetter, previousLetter, nextLetter);
            }
            else
            {
                processedText[i] = currentLetter; // Caracteres no árabes se mantienen igual
            }
        }

        return new string(processedText);
    }
    public void Start()
    {
        string textSoFar = ""; // Texto acumulado
        char[] input = { 'ا', 'ل', 'س', 'ل', 'ا', 'م', ' ', 'ع', 'ل', 'ي', 'ك', 'م' }; // Frase de ejemplo letra por letra

        // Simulamos la entrada letra a letra
        foreach (char letter in input)
        {
            textSoFar = ProcessLetterByLetter(textSoFar, letter);
            Debug.Log("Texto procesado hasta ahora: " + textSoFar);
        }
    }
}