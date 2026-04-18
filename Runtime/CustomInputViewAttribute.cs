using System;
using UnityEngine;

namespace UnityEngine.UIElements.Extension
{
    public class CustomInputViewAttribute : Attribute
    {

        public CustomInputViewAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public Type TargetType { get; set; }


    }
}
