using System;
using UnityEngine;
using Bindings;

namespace Unity
{

    [Serializable]
    public struct ConfigValue<T> : IEquatable<ConfigValue<T>>
    {
        [SerializeField]
        private ConfigValueKeyword keyword;

        [SerializeField]
        private T value;

        public ConfigValueKeyword Keyword
        {
            get => keyword;
            set
            {
                keyword = value;
                this.value = default;
            }
        }

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                keyword = ConfigValueKeyword.Undefined;
            }
        }
        public ConfigValue(ConfigValueKeyword keyword)
        {
            this.keyword = keyword;
            value = default;
        }

        public ConfigValue(T value)
        {
            this.keyword = ConfigValueKeyword.Undefined;
            this.value = value;
        }

        public ConfigValue(ConfigValueKeyword keyword, T value)
        {
            this.keyword = keyword;
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                if (keyword == ConfigValueKeyword.Null)
                    return true;
                return false;
            }
            if (obj is ConfigValue<T>)
            {
                return Equals((ConfigValue<T>)obj);
            }
            return false;
        }

        public bool Equals(ConfigValue<T> other)
        {
            if (other.keyword == keyword)
            {
                if (keyword == ConfigValueKeyword.Undefined)
                    return object.Equals(other.Value, Value);
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + keyword.GetHashCode();
                if (keyword != ConfigValueKeyword.Undefined && value != null)
                {
                    hash = hash * 23 + value.GetHashCode();
                }
                return hash;
            }
        }
        public override string ToString()
        {
            if (keyword != ConfigValueKeyword.Undefined)
                return keyword.ToString();

            return value == null ? string.Empty : value.ToString();
        }

        public static implicit operator ConfigValue<T>(T value)
        {
            return new ConfigValue<T>(value);
        }
        public static implicit operator ConfigValue<T>(ConfigValueKeyword keyword)
        {
            return new ConfigValue<T>(keyword);
        }
        public static implicit operator T(ConfigValue<T> configValue)
        {
            return configValue.Value;
        }

        public static bool operator ==(ConfigValue<T> a, ConfigValueKeyword keyword)
        {
            return a.keyword == keyword;
        }

        public static bool operator !=(ConfigValue<T> a, ConfigValueKeyword keyword)
        {
            return a.keyword != keyword;
        }

        public static bool operator ==(ConfigValue<T> a, ConfigValue<T> b)
        { 
            return object.Equals(a, b);
        }

        public static bool operator !=(ConfigValue<T> a, ConfigValue<T> b)
        {
            return !object.Equals( a , b);
        }


        private class ConfigValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter)
            {
                if (value == null)
                {
                    if (targetType == typeof(ConfigValue<T>))
                    {
                        return new ConfigValue<T>((T)default);
                    }

                    return null;
                }

                Type valueType = value.GetType();
                if (valueType != typeof(ConfigValue<T>))
                {
                    if (targetType == typeof(ConfigValue<T>))
                    {
                        if (valueType == typeof(T))
                        {
                            value = new ConfigValue<T>((T)value);
                        }
                    }
                    else if (targetType == typeof(T))
                    {
                        if (valueType == typeof(ConfigValue<T>))
                        {
                            value = ((ConfigValue<T>)value).Value;
                        }
                    }
                }
                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter)
            {
                return Convert(value, targetType, parameter);
            }
        }
    }

    public enum ConfigValueKeyword
    {
        /// <summary>
        /// 没有定义关键字, 值可用
        /// </summary>
        Undefined = 0,
        /// <summary>
        /// 没有值和关键字
        /// </summary>
        Null = 1,
    }


}
