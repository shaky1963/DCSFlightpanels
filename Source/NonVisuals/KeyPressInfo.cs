﻿using System;
using System.Collections.Generic;
using System.Text;
using ClassLibraryCommon;
using Newtonsoft.Json;

namespace NonVisuals
{
    [Serializable]
    public class KeyPressInfo
    {
        private KeyPressLength _lengthOfBreak = KeyPressLength.FiftyMilliSec;
        private KeyPressLength _lengthOfKeyPress = KeyPressLength.FiftyMilliSec;
        private HashSet<VirtualKeyCode> _virtualKeyCodes = new HashSet<VirtualKeyCode>();


        public int GetHash()
        {
            unchecked
            {
                var result = 0;
                foreach (var virtualKeyCode in _virtualKeyCodes)
                {
                    result = (result * 397) ^ virtualKeyCode.GetHashCode();
                }
                result = (result * 397) ^ _lengthOfBreak.GetHashCode();
                result = (result * 397) ^ _lengthOfKeyPress.GetHashCode();
                return result;
            }
        }

        public KeyPressLength LengthOfBreak
        {
            get => _lengthOfBreak;
            set => _lengthOfBreak = value;
        }

        public KeyPressLength LengthOfKeyPress
        {
            get => _lengthOfKeyPress;
            set => _lengthOfKeyPress = value;
        }

        public HashSet<VirtualKeyCode> VirtualKeyCodes
        {
            get => _virtualKeyCodes;
            set => _virtualKeyCodes = value;
        }

        [JsonIgnore]
        public string VirtualKeyCodesAsString
        {
            get
            {
                var result = new StringBuilder();
                if (_virtualKeyCodes.Count > 0)
                {
                    foreach (var virtualKeyCode in _virtualKeyCodes)
                    {
                        if (result.Length > 0)
                        {
                            result.Append(" + ");
                        }
                        result.Append(Enum.GetName(typeof(VirtualKeyCode), virtualKeyCode));
                    }
                }
                return result.ToString();
            }
        }
    }
}
