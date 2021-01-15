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

        [GiftDataFormatterMember("AListIntValue")]
        public List<int> AListIntValue;

        [GiftDataFormatterMember("AArrayListValue")]
        public ArrayList AArrayListValue;

        [GiftDataFormatterMember("AArrayValue")]
        public string[] AArrayValue;

        [GiftDataFormatterMember("AListListValue")]
        public List<List<string>> AListListValue;

        [GiftDataFormatterMember("AListArrayValue")]
        public List<Array> AListArrayValue;

        [GiftDataFormatterMember("AListIntArrayValue")]
        public List<int[]> AListIntArrayValue;

        [GiftDataFormatterMember("AListArraysValue")]
        public List<List<Array>> AListArraysValue;

        [GiftDataFormatterMember("AListArrayListValue")]
        public List<ArrayList> AListArrayListValue;

        [GiftDataFormatterMember("ADictionaryValue")]
        public Dictionary<int, string> ADictionaryValue;

        [GiftDataFormatterMember("ADictionaryListValue")]
        public Dictionary<string, List<int>> ADictionaryListValue;

        [GiftDataFormatterMember("AADictionaryValue2")]
        public Dictionary<string, Dictionary<string, int>> AADictionaryValue2;

        [GiftDataFormatterMember("AADictionaryValue3")]
        public Dictionary<int, int[]> AADictionaryValue3;

        [GiftDataFormatterMember("AObjectValue")]
        public ModelA AObjectValue;

        [GiftDataFormatterMember("AObjectListValue")]
        public List<ModelA> AObjectListValue;

        [GiftDataFormatterMember("AObjectDictionaryValue")]
        public Dictionary<int, ModelA> AObjectDictionaryValue;

        public int aaa;
    }


    public static void Run()
    {
        var model = new ModelA();
        model.aaa = 123445;
        model.AIntValue = 1;
        model.AEnumValue = EnumA.B;
        model.ABoolValue = false;
        model.ALongValue = 123456789101112;
        model.AFloatValue = 1.01f;
        model.AListIntValue = new List<int>() { 11, 111, 1111, 11111 };
        model.AArrayListValue = new ArrayList() { 1, "[a]bc", 5.0d, EnumA.C, true, new List<int>() { 1, 2 }, new string[] { "test,{", "code!!}" }, new List<EnumA>() { EnumA.B, EnumA.B }, new List<List<ArrayList>>() { new List<ArrayList>() { new ArrayList() { 1, "a", EnumA.C, new int[] { 10, 11, 12 } } } }, new Dictionary<string, List<Dictionary<List<int>, List<string>>>>() { { "abc", new List<Dictionary<List<int>, List<string>>>() { new Dictionary<List<int>, List<string>>() { { new List<int>() { 1, 2 }, new List<string>() { "xxx", "yyy" } } }, new Dictionary<List<int>, List<string>>() { { new List<int>() { 10, 11, 12 }, new List<string>() { "zzz", "www" } } } } } } };


        ModelA a = new ModelA();
        a.aaa = 10;
        a.AIntValue = 1;
        a.AEnumValue = EnumA.B;
        a.ABoolValue = false;
        a.ALongValue = 123456789101112;
        a.AFloatValue = 1.01f;
        a.ADoubleValue = 1.00123d;
        a.AStringValue = "a[}bcdefgh";
        a.AListIntValue = new List<int>() { 1, 3, 5, 7 };
        a.AArrayListValue = new ArrayList() { 1, "[a]bc", 5.0d, EnumA.C, true, new List<int>() { 1, 2 }, new string[] { "test,{", "code!!}" }, new List<EnumA>() { EnumA.B, EnumA.B }, new List<List<ArrayList>>() { new List<ArrayList>() { new ArrayList() { 1, "a", EnumA.C, new int[] { 10, 11, 12 } } } }, new Dictionary<string, List<Dictionary<List<int>, List<string>>>>() { { "abc", new List<Dictionary<List<int>, List<string>>>() { new Dictionary<List<int>, List<string>>() { { new List<int>() { 1, 2 }, new List<string>() { "xxx", "yyy" } } }, new Dictionary<List<int>, List<string>>() { { new List<int>() { 10, 11, 12 }, new List<string>() { "zzz", "www" } } } } } }, model };
        a.AArrayValue = new string[] { "hello,", "world!" };
        a.AListListValue = new List<List<string>>() { new List<string>() { "this", "is:;", "ok" }, new List<string>() { "new::", "world!" }, new List<string>() { ",good!" } };
        a.AListArrayValue = new List<Array>() { new string[] { "abc", "hello, world!" }, new int[] { 1, 2, 3 }, new ModelA[] { model, model } };
        a.AListIntArrayValue = new List<int[]>() { new int[] { 100, 200 }, new int[] { 300, 400, 600 } };
        a.AListArraysValue = new List<List<Array>>() { new List<Array>() { new int[] { 1, 2, 3 }, new string[] { "a", "b" } }, new List<Array>() { new List<int>[] { new List<int>() { 1, 2, 3 } } } };
        a.AListArrayListValue = new List<ArrayList>() { new ArrayList() { 123, 456.01f, "abc" }, new ArrayList() { 1, new List<int>() { 23, 45 }, new List<List<int>>() { new List<int>() { 1 }, new List<int>() { 2, 3, 4 } } }, new ArrayList() { new List<List<string[]>>() { new List<string[]>() { new string[] { "hhhhhhhhhhhhhh" } } } }, new ArrayList() { new int[] { 444, 555, 666 }, new string[] { "aaaaa", "b" } }, new ArrayList() { new List<Array>() { new int[] { 1 }, new string[] { "234" } }, new List<Dictionary<Array, Array>>() { new Dictionary<Array, Array>() { { new int[] { 2, 3 }, new string[] { "ac", "bd" } } } } }, new ArrayList() { new List<ModelA>() { model }, new Dictionary<int, ModelA>() { { 1, model }, { 2, model } } } };
        a.ADictionaryValue = new Dictionary<int, string>()
        {
            {1, "a"},
            {3, "abc"},
            {5, "abcde"},
            {7, "abcdefg"},
        };
        a.ADictionaryListValue = new Dictionary<string, List<int>>() { { "test1", new List<int>() { 1234, 244, 1 } }, { "test2", new List<int>() { 5, 9, 100 } } };
        a.AADictionaryValue2 = new Dictionary<string, Dictionary<string, int>>() { { "1800", new Dictionary<string, int>() { { "1", 2 }, { "2", 10 }, { "12", 20 } } }, { "3600", new Dictionary<string, int>() { { "11", 20 }, { "21", 100 } } } };
        a.AADictionaryValue3 = new Dictionary<int, int[]>() { { 1, new int[] { 1, 2, 3 } }, { 2, new int[] { 11, 12, 13, 14, 15 } } };
        a.AObjectValue = model;
        a.AObjectListValue = new List<ModelA>() { model, model };
        a.AObjectDictionaryValue = new Dictionary<int, ModelA>() { { 12, model }, { 34, model } };

        var formatterString = GiftDataFormatter.Serialize(a);
        Console.WriteLine("run Serialize a formatter string => " + formatterString);


        ModelA b = GiftDataFormatter.Deserialize<ModelA>(formatterString);
        var newFormatterString = GiftDataFormatter.Serialize(b);
        Console.WriteLine("run Serialize b formatter string => " + newFormatterString);
        bool isModelEqual = newFormatterString == formatterString;
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
