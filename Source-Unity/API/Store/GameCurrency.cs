using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole
{

    [CreateAssetMenu(fileName = "currency", menuName = "Swole/Currency", order = 2)]
    public class GameCurrency : ScriptableObject
    {

        public const string SwoleBucksID = "swole-bucks";

        [SerializeField]
        protected string id;
        public string ID => string.IsNullOrWhiteSpace(id) ? name : id;

        [SerializeField]
        protected bool valueIsInteger = true;
        public bool ValueIsInteger => valueIsInteger;

        [SerializeField]
        protected string abbreviation;
        public string Abbreviation => abbreviation;

        [SerializeField]
        protected string prefix;
        public string Prefix => string.IsNullOrWhiteSpace(prefix) ? abbreviation : prefix;

        [SerializeField]
        protected string suffix;
        public string Suffix => string.IsNullOrWhiteSpace(suffix) ? abbreviation : suffix;

        public string AsStringWithPrefix(int amount) => valueIsInteger ? $"{Prefix} {amount}" : AsStringWithPrefix((float)amount);
        public string AsStringWithSuffix(int amount) => valueIsInteger ? $"{amount} {Prefix}" : AsStringWithPrefix((float)amount);

        public string AsStringWithPrefix(float amount) => valueIsInteger ? AsStringWithPrefix((int)amount) : $"{Suffix} {amount:F2}";
        public string AsStringWithSuffix(float amount) => valueIsInteger ? AsStringWithSuffix((int)amount) : $"{amount:F2} {Suffix}"; 
    }

}
