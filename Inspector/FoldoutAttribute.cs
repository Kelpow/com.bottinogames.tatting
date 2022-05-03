/*
 * Code for the foldout property drawer was taken nearly wholesale from: https://github.com/giuliano-marinelli/UnityFoldoutDecorator
 * 
 * MIT License
 * 
 * Copyright (c) 2021 Giuliano Marinelli
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */


using UnityEngine;
namespace Tatting
{
    public class FoldoutAttribute : PropertyAttribute
    {
        public string name;
        public bool foldEverything;
        public bool readOnly;

        /// <summary>Adds the property to the specified foldout group.</summary>
        /// <param name="name">Name of the foldout group.</param>
        /// <param name="foldEverything">Toggle to put all properties to the specified group</param>
        /// <param name="readOnly">Toggle to put all properties to the specified group</param>
        public FoldoutAttribute(string name, bool foldEverything = true, bool readOnly = false)
        {
            this.foldEverything = foldEverything;
            this.name = name;
            this.readOnly = readOnly;
        }
    }
}