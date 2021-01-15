using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Reflection;
using System.Globalization;

public class GiftDataFormatterDefine
{
    public const string kObjectMemberSeperator = ";";
    public const string kMemberAliasSeperator = ":";

    public const string kStringStart = "“";
    public const string kStringEnd = "”";

    public const string kSubItemTypeSeperator = "#";
    public const string kCollectionItemSeperator = ",";

    public const string kArrayOrListStart = "[";
    public const string kArrayOrListEnd = "]";

    public const string kDictionaryStart = "{";
    public const string kDictionaryKVSeperator = "::";
    public const string kDictionaryEnd = "}";
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class GiftDataFormatterModelAttribute : Attribute
{
    public readonly string MemberSeperator;

    public GiftDataFormatterModelAttribute(string memberSeperator = GiftDataFormatterDefine.kObjectMemberSeperator)
    {
        this.MemberSeperator = memberSeperator;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class GiftDataFormatterMemberAttribute : Attribute
{
    public readonly string MemberAlias;
    public String MemberName;
    public Object MemberValue;
    public Type MemberType;

    public GiftDataFormatterMemberAttribute(string aliasValue)
    {
        MemberAlias = aliasValue;
    }
}

public class GiftDataFormatter
{

    struct SupportTypeItem
    {
        public Type type;
        public int value;

        public SupportTypeItem(Type type, int value)
        {
            this.type = type;
            this.value = value;
        }
    }

    class SupportTypes
    {
        // int or enum
        public static SupportTypeItem Int = new SupportTypeItem(typeof(int), 0);
        public static SupportTypeItem Bool = new SupportTypeItem(typeof(bool), 1);
        public static SupportTypeItem Float = new SupportTypeItem(typeof(float), 2);
        public static SupportTypeItem Double = new SupportTypeItem(typeof(double), 3);
        public static SupportTypeItem String = new SupportTypeItem(typeof(string), 4);
        public static SupportTypeItem Long = new SupportTypeItem(typeof(long), 5);
        public static SupportTypeItem DateTime = new SupportTypeItem(typeof(DateTime), 20);
        public static SupportTypeItem ArrayList = new SupportTypeItem(typeof(ArrayList), 21);
        public static SupportTypeItem Array = new SupportTypeItem(typeof(Array), 22);
        public static SupportTypeItem IGenericList = new SupportTypeItem(typeof(List<>), 23);
        public static SupportTypeItem IGenericDictionary = new SupportTypeItem(typeof(Dictionary<,>), 24);

        public static Type IListType = typeof(IList);
        public static Type IDictionaryType = typeof(IDictionary);

        private static List<SupportTypeItem> m_SupportTypes;
        static SupportTypes()
        {
            m_SupportTypes = new List<SupportTypeItem>()
            {
                Int, Bool, Float, Double, String, Long, DateTime, ArrayList, Array, IGenericList, IGenericDictionary
            };
        }

        public static T Cast<T>(object o)
        {
            return (T)o;
        }

        public static Type ConvertIntValueToType(int value)
        {
            Type type = typeof(object);
            foreach (var item in m_SupportTypes)
            {
                if (item.value == value)
                {
                    type = item.type;
                    break;
                }
            }
            return type;
        }

        public static int ConvertTypeToIntValue(Type type)
        {
            int value = type.IsEnum ? Int.value : -1;
            foreach (var item in m_SupportTypes)
            {
                if (item.type == type)
                {
                    value = item.value;
                    break;
                }
            }

            return value;
        }
    }

    private static NumberFormatInfo number_format = NumberFormatInfo.InvariantInfo;
    private static StringBuilder m_SerializeBuilder = new StringBuilder();
    private static StringBuilder m_DeserializeBuilder = new StringBuilder();
    private static string m_MemberSeperator;
    private static Dictionary<Type, GiftDataFormatterModelAttribute> m_FormatterSeperatorAttributeMap = new Dictionary<Type, GiftDataFormatterModelAttribute>();
    private static Dictionary<Type, List<GiftDataFormatterMemberAttribute>> m_FormatterAliasAttributesMap = new Dictionary<Type, List<GiftDataFormatterMemberAttribute>>();

    public static string Serialize(Object o)
    {
        if (o == null) return string.Empty;

        var memberAttrs = FindDataMemberAttributes(o.GetType(), o);
        if (memberAttrs == null || memberAttrs.Count == 0)
        {
            // UnityEngine.Debug.LogError($"[GiftDataFormatter]{o.GetType()} can not Serialize: do not has '{typeof(GiftDataFormatterMemberAttribute)}'");
            return string.Empty;
        }

        m_MemberSeperator = FindDataMemberSeperator(o.GetType());
        m_SerializeBuilder.Clear();
        bool isFirstItem = true;
        foreach (var memberAttr in memberAttrs)
        {
            if (!isFirstItem)
            {
                m_SerializeBuilder.Append(m_MemberSeperator);
            }
            m_SerializeBuilder.Append(memberAttr.MemberAlias);
            m_SerializeBuilder.Append(GiftDataFormatterDefine.kMemberAliasSeperator);
            WriteObjectValue(memberAttr.MemberType, memberAttr.MemberValue, m_SerializeBuilder);
            isFirstItem = false;
        }

        return m_SerializeBuilder.ToString();
    }

    public static T Deserialize<T>(string serializedObject) where T : new()
    {
        var memberAttrs = FindDataMemberAttributes(typeof(T));
        if (memberAttrs == null || memberAttrs.Count == 0)
        {
            // UnityEngine.Debug.LogError($"[GiftDataFormatter]{typeof(T)} can not Deserialize: do not has '{typeof(GiftDataFormatterMemberAttribute)}'");
            return default(T);
        }

        m_MemberSeperator = FindDataMemberSeperator(typeof(T));
        T tObject = new T();
        Type tType = tObject.GetType();
        List<string> formatterMemberArray = new List<string>();
        SplitOrganizedString(serializedObject, m_MemberSeperator, ref formatterMemberArray);
        foreach (var formatterMember in formatterMemberArray)
        {
            foreach (var memberAttr in memberAttrs)
            {
                m_DeserializeBuilder.Clear();
                m_DeserializeBuilder.Append(memberAttr.MemberAlias);
                m_DeserializeBuilder.Append(GiftDataFormatterDefine.kMemberAliasSeperator);
                string prefix = m_DeserializeBuilder.ToString();
                if (formatterMember.StartsWith(prefix))
                {
                    string valueString = formatterMember.Replace(prefix, string.Empty);
                    var valueType = memberAttr.MemberType;

                    var field = tType.GetField(memberAttr.MemberName);
                    if (field != null && field.DeclaringType != tType)
                    {
                        field = field.DeclaringType.GetField(memberAttr.MemberName);
                    }
                    PropertyInfo property = null;
                    if (field == null)
                    {
                        property = tType.GetProperty(memberAttr.MemberName);
                        if (property != null && property.DeclaringType != tType)
                        {
                            property = property.DeclaringType.GetProperty(memberAttr.MemberName);
                        }
                    }
                    if (field == null && property == null)
                    {
                        // not found, maybe filed or property areas did changed
                        continue;
                    }

                    object valueObject = ReadObjectValue(valueType, valueString);
                    if (field != null)
                    {
                        field.SetValue(tObject, valueObject);
                    }
                    else if (property != null)
                    {
                        property.SetValue(tObject, valueObject);
                    }
                }
            }
        }
        return tObject;
    }

    private static void WriteObjectValue(Type valueType, object originValue, StringBuilder writer)
    {
        if (valueType.IsEnum || valueType == SupportTypes.Int.type)
        {
            writer.Append((int)originValue);
        }
        else if (valueType == SupportTypes.Bool.type)
        {
            writer.Append(((bool)originValue) ? 1 : 0);
        }
        else if (valueType == SupportTypes.Float.type)
        {
            WriteObjectValue(SupportTypes.String.type, Convert.ToString((float)(originValue), number_format), writer);
        }
        else if (valueType == SupportTypes.Double.type)
        {
            WriteObjectValue(SupportTypes.String.type, Convert.ToString((double)(originValue), number_format), writer);
        }
        else if (valueType == SupportTypes.String.type)
        {
            writer.Append(GiftDataFormatterDefine.kStringStart);
            writer.Append((string)originValue);
            writer.Append(GiftDataFormatterDefine.kStringEnd);
        }
        else if (valueType == SupportTypes.Long.type)
        {
            writer.Append((long)originValue);
        }
        else if (valueType == SupportTypes.DateTime.type)
        {
            double timestamp = ConvertDateTimeToTimestamp((DateTime)originValue);
            writer.Append(Convert.ToString(timestamp, number_format));
        }
        else if (SupportTypes.IListType.IsAssignableFrom(valueType))
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.type.isassignablefrom
            writer.Append(GiftDataFormatterDefine.kArrayOrListStart);
            var collection = (ICollection)originValue;
            bool isFirstItem = true;
            foreach (var item in collection)
            {
                if (!isFirstItem)
                {
                    writer.Append(GiftDataFormatterDefine.kCollectionItemSeperator);
                }

                WriteObjectValue(item.GetType(), item, writer);
                writer.Append(GiftDataFormatterDefine.kSubItemTypeSeperator);
                Type writeItemType = GetSupportItemType(item.GetType());
                WriteObjectValue(SupportTypes.Int.type, SupportTypes.ConvertTypeToIntValue(writeItemType), writer);

                isFirstItem = false;
            }
            writer.Append(GiftDataFormatterDefine.kArrayOrListEnd);
        }
        else if (SupportTypes.IDictionaryType.IsAssignableFrom(valueType))
        {
            writer.Append(GiftDataFormatterDefine.kDictionaryStart);
            bool isFirstItem = true;
            var dic = (IDictionary)originValue;
            foreach (var key in dic.Keys)
            {
                if (!isFirstItem)
                {
                    writer.Append(GiftDataFormatterDefine.kCollectionItemSeperator);
                }

                // key
                WriteObjectValue(key.GetType(), key, writer);
                writer.Append(GiftDataFormatterDefine.kSubItemTypeSeperator);
                Type dicKeyType = GetSupportItemType(key.GetType());
                WriteObjectValue(SupportTypes.Int.type, SupportTypes.ConvertTypeToIntValue(dicKeyType), writer);

                writer.Append(GiftDataFormatterDefine.kDictionaryKVSeperator);

                //value
                var value = dic[key];
                WriteObjectValue(value.GetType(), value, writer);
                writer.Append(GiftDataFormatterDefine.kSubItemTypeSeperator);
                Type dicValueType = GetSupportItemType(value.GetType());
                WriteObjectValue(SupportTypes.Int.type, SupportTypes.ConvertTypeToIntValue(dicValueType), writer);

                isFirstItem = false;

            }
            writer.Append(GiftDataFormatterDefine.kDictionaryEnd);
        }
    }

    private static Type GetSupportItemType(Type originType)
    {
        if (originType == SupportTypes.ArrayList.type)
        {
            originType = SupportTypes.ArrayList.type;
        }
        else if (originType.IsArray)
        {
            originType = SupportTypes.Array.type;
        }
        else if (SupportTypes.IListType.IsAssignableFrom(originType))
        {
            originType = SupportTypes.IGenericList.type;
        }
        else if (SupportTypes.IDictionaryType.IsAssignableFrom(originType))
        {
            originType = SupportTypes.IGenericDictionary.type;
        }
        return originType;
    }

    private static object ReadObjectValue(Type valueType, string valueString)
    {
        object valueObject = null;
        if (valueType == SupportTypes.Int.type || valueType.IsEnum)
        {
            valueObject = Convert.ToInt32(valueString);
        }
        else if (valueType == SupportTypes.Bool.type)
        {
            valueObject = Convert.ToInt32(valueString) > 0;
        }
        else if (valueType == SupportTypes.Float.type)
        {
            valueString = valueString.Substring(GiftDataFormatterDefine.kStringStart.Length, valueString.Length - GiftDataFormatterDefine.kStringStart.Length - GiftDataFormatterDefine.kStringEnd.Length);
            valueObject = Convert.ToSingle(valueString, number_format);
        }
        else if (valueType == SupportTypes.Double.type)
        {
            valueString = valueString.Substring(GiftDataFormatterDefine.kStringStart.Length, valueString.Length - GiftDataFormatterDefine.kStringStart.Length - GiftDataFormatterDefine.kStringEnd.Length);
            valueObject = Convert.ToDouble(valueString, number_format);
        }
        else if (valueType == SupportTypes.String.type)
        {
            valueString = valueString.Substring(GiftDataFormatterDefine.kStringStart.Length, valueString.Length - GiftDataFormatterDefine.kStringStart.Length - GiftDataFormatterDefine.kStringEnd.Length);
            valueObject = valueString;
        }
        else if (valueType == SupportTypes.Long.type)
        {
            valueObject = Convert.ToInt64(valueString);
        }
        else if (valueType == SupportTypes.DateTime.type)
        {
            double timestamp = Convert.ToDouble(valueString, number_format);
            valueObject = ConvertTimestampToDateTime(timestamp);
        }
        else if (SupportTypes.IListType.IsAssignableFrom(valueType))
        {
            valueString = valueString.Substring(GiftDataFormatterDefine.kArrayOrListStart.Length, valueString.Length - GiftDataFormatterDefine.kArrayOrListStart.Length - GiftDataFormatterDefine.kArrayOrListEnd.Length);
            List<string> listValueString = new List<string>();
            SplitOrganizedString(valueString, GiftDataFormatterDefine.kCollectionItemSeperator, ref listValueString);
            IList list = null;
            if (valueType.IsArray || valueType == SupportTypes.Array.type)
            {
                var arrayItemType = FindArrayOrListItemType(valueType, listValueString[0]);
                list = Array.CreateInstance(arrayItemType, listValueString.Count);
            }
            else
            {
                list = (IList)Activator.CreateInstance(valueType);
            }

            for (int index = 0; index < listValueString.Count; index++)
            {
                var result = SplitMemberItemString(listValueString[index]);
                var listItemValueString = result.Item1;
                var listItemType = FindArrayOrListItemType(valueType, listValueString[index]);
                if (valueType.IsArray || valueType == SupportTypes.Array.type)
                {
                    list[index] = ReadObjectValue(listItemType, listItemValueString);
                }
                else
                {
                    list.Add(ReadObjectValue(listItemType, listItemValueString));
                }
            }
            valueObject = list;
        }
        else if (SupportTypes.IDictionaryType.IsAssignableFrom(valueType))
        {
            var dicString = valueString.Substring(GiftDataFormatterDefine.kDictionaryStart.Length, valueString.Length - GiftDataFormatterDefine.kDictionaryStart.Length - GiftDataFormatterDefine.kDictionaryEnd.Length);
            List<string> dicItemStringList = new List<string>();
            SplitOrganizedString(dicString, GiftDataFormatterDefine.kCollectionItemSeperator, ref dicItemStringList);
            IDictionary dic = (IDictionary)Activator.CreateInstance(valueType);
            foreach (var kvString in dicItemStringList)
            {
                List<string> kvStringList = new List<string>();
                SplitOrganizedString(kvString, GiftDataFormatterDefine.kDictionaryKVSeperator, ref kvStringList);
                Type dicKeyType = FindDictionaryItemType(valueType, kvStringList[0], true);
                Type dicValueType = FindDictionaryItemType(valueType, kvStringList[1], false);
                dic[ReadObjectValue(dicKeyType, SplitMemberItemString(kvStringList[0]).Item1)] = ReadObjectValue(dicValueType, SplitMemberItemString(kvStringList[1]).Item1);
            }
            valueObject = dic;
        }
        return valueObject;
    }

    private static void SplitOrganizedString(string s, string Seperator, ref List<string> result)
    {
        int arrayOrListDepth = 0;
        int dictionaryDepth = 0;
        int stringDepth = 0;
        int startIndex = 0;

        while (!s.Substring(startIndex).StartsWith(Seperator))
        {
            //current 's' is just origin 's' last part.
            if (startIndex >= s.Length)
            {
                startIndex = s.Length;
                break;
            }

            if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kArrayOrListStart))
            {
                arrayOrListDepth++;
                startIndex += GiftDataFormatterDefine.kArrayOrListStart.Length;
            }
            else if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kDictionaryStart))
            {
                dictionaryDepth++;
                startIndex += GiftDataFormatterDefine.kDictionaryStart.Length;
            }
            else if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kStringStart))
            {
                stringDepth++;
                startIndex += GiftDataFormatterDefine.kStringStart.Length;
            }
            else
            {
                startIndex++;
            }

            while (arrayOrListDepth > 0 || dictionaryDepth > 0 || stringDepth > 0)
            {
                bool isOutOfString = stringDepth == 0;
                if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kArrayOrListStart))
                {
                    if (isOutOfString) arrayOrListDepth++;
                    startIndex += GiftDataFormatterDefine.kArrayOrListStart.Length;
                }
                else if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kArrayOrListEnd))
                {
                    if (isOutOfString) arrayOrListDepth--;
                    startIndex += GiftDataFormatterDefine.kArrayOrListEnd.Length;
                }
                else if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kDictionaryStart))
                {
                    if (isOutOfString) dictionaryDepth++;
                    startIndex += GiftDataFormatterDefine.kDictionaryStart.Length;
                }
                else if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kDictionaryEnd))
                {
                    if (isOutOfString) dictionaryDepth--;
                    startIndex += GiftDataFormatterDefine.kDictionaryEnd.Length;
                }
                else if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kStringStart))
                {
                    stringDepth++;
                    startIndex += GiftDataFormatterDefine.kStringStart.Length;
                }
                else if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kStringEnd))
                {
                    stringDepth--;
                    startIndex += GiftDataFormatterDefine.kStringEnd.Length;
                }
                else
                {
                    startIndex++;
                }
            }
        }

        if (startIndex > 0)
        {
            var splitPart = s.Substring(0, startIndex);
            var newStringStartIndex = Math.Min(startIndex + Seperator.Length, s.Length);
            s = s.Substring(newStringStartIndex);
            result.Add(splitPart);
        }

        if (!string.IsNullOrEmpty(s))
        {
            SplitOrganizedString(s, Seperator, ref result);
        }

    }

    private static (string, int) SplitMemberItemString(string originMemberString)
    {
        List<string> valueAndTypeStringArray = new List<string>();
        SplitOrganizedString(originMemberString, GiftDataFormatterDefine.kSubItemTypeSeperator, ref valueAndTypeStringArray);
        var typeIntString = valueAndTypeStringArray[1];
        var typeInt = Convert.ToInt32(typeIntString);
        var valueString = valueAndTypeStringArray[0];
        return (valueString, typeInt);
    }

    private static Type FindDictionaryType(Type dictionaryType, string kvString)
    {
        List<string> kvStringArray = new List<string>();
        SplitOrganizedString(kvString, GiftDataFormatterDefine.kDictionaryKVSeperator, ref kvStringArray);
        Type keyType = FindDictionaryItemType(dictionaryType, kvStringArray[0], true);
        Type valueType = FindDictionaryItemType(dictionaryType, kvStringArray[1], false);
        return SupportTypes.IGenericDictionary.type.MakeGenericType(keyType, valueType);
    }

    private static Type FindDictionaryItemType(Type dictionaryType, string itemString, bool isKey)
    {
        var valueAndType = SplitMemberItemString(itemString);
        var value = valueAndType.Item1;
        var type = SupportTypes.ConvertIntValueToType(valueAndType.Item2);
        if (dictionaryType == SupportTypes.IGenericDictionary.type)
        {
            if (SupportTypes.IListType.IsAssignableFrom(type))
            {
                var subListString = value.Substring(GiftDataFormatterDefine.kArrayOrListStart.Length, value.Length - GiftDataFormatterDefine.kArrayOrListStart.Length - GiftDataFormatterDefine.kArrayOrListEnd.Length);
                List<string> listValueString = new List<string>();
                SplitOrganizedString(subListString, GiftDataFormatterDefine.kCollectionItemSeperator, ref listValueString);
                var listItemType = FindArrayOrListItemType(type, listValueString[0]);
                if (type == SupportTypes.Array.type)
                {
                    type = SupportTypes.Array.type;
                }
                else if (listItemType == SupportTypes.ArrayList.type)
                {
                    type = SupportTypes.ArrayList.type;
                }
                else
                {
                    type = type.MakeGenericType(listItemType);
                }
            }
            else if (SupportTypes.IDictionaryType.IsAssignableFrom(type))
            {
                var subDicString = value.Substring(GiftDataFormatterDefine.kDictionaryStart.Length, value.Length - GiftDataFormatterDefine.kDictionaryStart.Length - GiftDataFormatterDefine.kDictionaryEnd.Length);
                type = FindDictionaryType(SupportTypes.IGenericDictionary.type, subDicString);
            }
        }
        else
        {
            // has real type
            var arguments = dictionaryType.GetGenericArguments();
            type = isKey ? arguments[0] : arguments[1];
        }

        return type;
    }

    private static Type FindArrayOrListItemType(Type arrayOrListType, string originItemString)
    {
        var itemValueType = SplitMemberItemString(originItemString);
        int listItemTypeInt = itemValueType.Item2;
        string listItemValueString = itemValueType.Item1;
        Type listItemType = SupportTypes.ConvertIntValueToType(listItemTypeInt);

        if (arrayOrListType == SupportTypes.ArrayList.type
            || arrayOrListType == SupportTypes.IGenericList.type
            || arrayOrListType == SupportTypes.Array.type
            )
        {
            if (listItemType == SupportTypes.IGenericList.type)
            {
                List<string> listItemListValueString = new List<string>();
                listItemValueString = listItemValueString.Substring(GiftDataFormatterDefine.kArrayOrListStart.Length, listItemValueString.Length - GiftDataFormatterDefine.kArrayOrListStart.Length - GiftDataFormatterDefine.kArrayOrListEnd.Length);
                SplitOrganizedString(listItemValueString, GiftDataFormatterDefine.kCollectionItemSeperator, ref listItemListValueString);
                var firstListItem = listItemListValueString[0];
                Type listItemListItemType = FindArrayOrListItemType(listItemType, firstListItem);
                listItemType = listItemType.MakeGenericType(listItemListItemType);
            }
            else if (listItemType == SupportTypes.IGenericDictionary.type)
            {
                listItemType = FindDictionaryType(SupportTypes.IGenericDictionary.type, listItemValueString.Substring(GiftDataFormatterDefine.kDictionaryStart.Length, listItemValueString.Length - GiftDataFormatterDefine.kDictionaryStart.Length - GiftDataFormatterDefine.kDictionaryEnd.Length));
            }
        }
        else
        {
            // https://www.codeproject.com/Tips/5267157/How-to-Get-a-Collection-Element-Type-Using-Reflect
            if (listItemType == SupportTypes.IGenericList.type || listItemType == SupportTypes.IGenericDictionary.type)
            {
                var etype = typeof(IEnumerable<>);
                foreach (var bt in arrayOrListType.GetInterfaces())
                {
                    if (bt.IsGenericType && bt.GetGenericTypeDefinition() == etype)
                    {
                        listItemType = bt.GetGenericArguments()[0];
                        break;
                    }
                }
            }

            if (listItemType == SupportTypes.Array.type)
            {
                foreach (var prop in arrayOrListType.GetProperties())
                {
                    if ("Item" == prop.Name && typeof(object) != prop.PropertyType)
                    {
                        var ipa = prop.GetIndexParameters();
                        if (1 == ipa.Length && typeof(int) == ipa[0].ParameterType)
                        {
                            return prop.PropertyType;
                        }
                    }
                }
            }

        }

        return listItemType;
    }

    private static double ConvertDateTimeToTimestamp(DateTime time)
    {
        return (time.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    }

    private static DateTime ConvertTimestampToDateTime(double sec)
    {
        DateTime utc1970 = new DateTime(1970, 1, 1);
        return utc1970.AddSeconds(sec);
    }

    private static string FindDataMemberSeperator(Type oType)
    {
        GiftDataFormatterModelAttribute attribute;
        if (!m_FormatterSeperatorAttributeMap.TryGetValue(oType, out attribute))
        {
            attribute = (GiftDataFormatterModelAttribute)Attribute.GetCustomAttribute(oType, typeof(GiftDataFormatterModelAttribute));
            if (attribute != null)
            {
                m_FormatterSeperatorAttributeMap[oType] = attribute;
            }
        }

        if (attribute == null)
        {
            return GiftDataFormatterDefine.kObjectMemberSeperator;
        }
        else
        {
            return attribute.MemberSeperator;
        }
    }

    private static List<GiftDataFormatterMemberAttribute> FindDataMemberAttributes(Type oType, Object o = null)
    {
        List<GiftDataFormatterMemberAttribute> result;
        if (!m_FormatterAliasAttributesMap.TryGetValue(oType, out result))
        {
            result = new List<GiftDataFormatterMemberAttribute>();

            var properties = oType.GetProperties();
            foreach (var p in properties)
            {
                var attr = (GiftDataFormatterMemberAttribute)Attribute.GetCustomAttribute(p, typeof(GiftDataFormatterMemberAttribute));
                if (attr != null)
                {
                    attr.MemberName = p.Name;
                    attr.MemberType = p.PropertyType;
                    if (o != null)
                    {
                        attr.MemberValue = p.GetValue(o);
                    }
                    result.Add(attr);
                }
            }

            var fields = oType.GetFields();
            foreach (var f in fields)
            {
                var attr = (GiftDataFormatterMemberAttribute)Attribute.GetCustomAttribute(f, typeof(GiftDataFormatterMemberAttribute));
                if (attr != null)
                {
                    attr.MemberName = f.Name;
                    attr.MemberType = f.FieldType;
                    if (o != null)
                    {
                        attr.MemberValue = f.GetValue(o);
                    }
                    result.Add(attr);
                }
            }
        }
        return result;
    }

}
