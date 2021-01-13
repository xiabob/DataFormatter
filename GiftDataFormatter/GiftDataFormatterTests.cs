using System;
using System.Collections;
using System.Collections.Generic;

public class GiftDataFormatterTests
{

    public enum EnumA
    {
        A,
        B,
        C,
    }

    [GiftDataFormatterModel()]
    public class ModelA
    {
        [GiftDataFormatterMember("i")]
        public int AIntValue;

        [GiftDataFormatterMember("e")]
        public EnumA AEnumValue;

        [GiftDataFormatterMember("b")]
        public bool ABoolValue;

        [GiftDataFormatterMember("l")]
        public long ALongValue;

        [GiftDataFormatterMember("f")]
        public float AFloatValue;

        [GiftDataFormatterMember("d")]
        public double ADoubleValue;

        [GiftDataFormatterMember("s")]
        public string AStringValue;

        [GiftDataFormatterMember("aliv")]
        public List<int> AListIntValue;

        [GiftDataFormatterMember("aalv")]
        public ArrayList AArrayListValue;

        // [GiftDataFormatterMember("adv")]
        public Dictionary<int, string> ADictionaryValue;

        public int aaa;
    }

    public static void Run()
    {
        ModelA a = new ModelA();
        a.aaa = 10;
        a.AIntValue = 1;
        a.AEnumValue = EnumA.B;
        a.ABoolValue = false;
        a.ALongValue = 123456789101112;
        a.AFloatValue = 1.01f;
        a.ADoubleValue = 1.00123d;
        a.AStringValue = "abcdefgh";
        a.AListIntValue = new List<int>() { 1, 3, 5, 7 };
        a.AArrayListValue = new ArrayList(){1, "abc", 5.0d, EnumA.C, true};
        // a.ADictionaryValue = new Dictionary<int, string>()
        // {
        //     {1, "a"},
        //     {3, "abc"},
        //     {5, "abcde"},
        //     {7, "abcdefg"},
        // };
        var formatterString = GiftDataFormatter.Serialize(a);
        if (formatterString != "i1;e1;b0;l123456789101112;f1.01;d1.00123;s/abcdefgh")
        {
            Console.WriteLine("run Serialize formatter string => " + formatterString);
        }



        ModelA b = GiftDataFormatter.Deserialize<ModelA>(formatterString);
        bool isModelEqual =
        b != a &&
        b.aaa == 0 &&
        b.AIntValue == a.AIntValue &&
        b.AEnumValue == a.AEnumValue &&
        b.ABoolValue == a.ABoolValue &&
        b.ALongValue == a.ALongValue &&
        b.AFloatValue == a.AFloatValue &&
        b.ADoubleValue == a.ADoubleValue &&
        b.AStringValue == a.AStringValue;
        if (isModelEqual)
        {
            Console.WriteLine("run Deserialize success!");
        }
        else
        {
            Console.WriteLine("run Deserialize failed!");
        }
    }

}
