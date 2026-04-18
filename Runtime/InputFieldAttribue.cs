using System;
using UnityEngine;

namespace UnityEngine.UIElements.Extension
{

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InputFieldAttribue : Attribute
    {
        public bool IsElement { get; set; }
    }
}
