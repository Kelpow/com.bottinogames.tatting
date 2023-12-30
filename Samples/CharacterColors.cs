using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterColors : Tatting.DirectDrawEffect
{
    public ColoredCharacter[] coloredCharacters = new ColoredCharacter[0];

    private Dictionary<char, Color> colorDict;

    private static int CHAR_COLOR_ID = Shader.PropertyToID("_Color");

    protected override void Awake()
    {
        base.Awake();

        colorDict = new Dictionary<char, Color>();
        foreach (var cc in coloredCharacters)
        {
            colorDict.Add(cc.character, cc.color);
        }
    }

    protected override void TextEffect(ref MaterialPropertyBlock materialPropertyBlock, int i, char c)
    {
        if (colorDict.TryGetValue(c, out Color col))
            materialPropertyBlock.SetColor(CHAR_COLOR_ID, col);
    }

    [System.Serializable]
    public struct ColoredCharacter
    {
        public char character;
        public Color color;
    }
}
