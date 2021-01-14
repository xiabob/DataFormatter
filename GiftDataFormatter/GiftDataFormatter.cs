using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Reflection;
using System.Globalization;

public class GiftDataFormatterDefine
{
    public const string kObjectMemberSeperator = ";;";
    public const string kMemberAliasSeperator = "::";
    public const string kArrayOrListStart = "[";
    public const string kArrayOrListItemSeperator = ",,";
    public const string kArrayOrListItemTypeSeperator = "##";
    public const string kArrayOrListEnd = "]";

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
        public static SupportTypeItem DateTime = new SupportTypeItem(typeof(DateTime), 6);
        public static SupportTypeItem ArrayList = new SupportTypeItem(typeof(ArrayList), 7);
        public static SupportTypeItem Array = new SupportTypeItem(typeof(Array), 8);
        public static SupportTypeItem IList = new SupportTypeItem(typeof(IList), 9);
        public static SupportTypeItem IGenericList = new SupportTypeItem(typeof(List<>), 10);

        private static List<SupportTypeItem> m_SupportTypes;
        static SupportTypes()
        {
            m_SupportTypes = new List<SupportTypeItem>()
            {
                Int, Bool, Float, Double, String, Long, DateTime, ArrayList, Array, IList,IGenericList
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
    private static StringBuilder m_ParseObjectValueBuilder = new StringBuilder();
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
        var formatterMemberArray = serializedObject.Split(m_MemberSeperator, StringSplitOptions.None);
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
            writer.Append(Convert.ToString((float)(originValue), number_format));
        }
        else if (valueType == SupportTypes.Double.type)
        {
            writer.Append(Convert.ToString((double)(originValue), number_format));
        }
        else if (valueType == SupportTypes.String.type)
        {
            writer.Append((string)originValue);
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
        else if (valueType == SupportTypes.ArrayList.type
                || SupportTypes.IList.type.IsAssignableFrom(valueType))
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.type.isassignablefrom
            writer.Append(GiftDataFormatterDefine.kArrayOrListStart);
            var collection = (ICollection)originValue;
            bool isFirstItem = true;
            foreach (var item in collection)
            {
                if (!isFirstItem)
                {
                    writer.Append(GiftDataFormatterDefine.kArrayOrListItemSeperator);
                }
                WriteObjectValue(item.GetType(), item, writer);
                writer.Append(GiftDataFormatterDefine.kArrayOrListItemTypeSeperator);
                Type writeItemType = item.GetType();
                if (writeItemType == SupportTypes.ArrayList.type)
                {
                    writeItemType = SupportTypes.ArrayList.type;
                }
                else if (writeItemType.IsArray)
                {
                    writeItemType = SupportTypes.Array.type;
                }
                else if (SupportTypes.IList.type.IsAssignableFrom(writeItemType))
                {
                    writeItemType = SupportTypes.IGenericList.type;
                }
                WriteObjectValue(SupportTypes.Int.type, SupportTypes.ConvertTypeToIntValue(writeItemType), writer);
                isFirstItem = false;
            }
            writer.Append(GiftDataFormatterDefine.kArrayOrListEnd);
        }
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
            valueObject = Convert.ToSingle(valueString, number_format);
        }
        else if (valueType == SupportTypes.Double.type)
        {
            valueObject = Convert.ToDouble(valueString, number_format);
        }
        else if (valueType == SupportTypes.String.type)
        {
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
        else if (SupportTypes.IList.type.IsAssignableFrom(valueType))
        {
            valueString = valueString.Substring(1, valueString.Length - 2);
            List<string> listValueString = new List<string>();
            SplitArrayOrListString(valueString, ref listValueString);
            IList list = null;
            if (valueType.IsArray || valueType == SupportTypes.Array.type)
            {
                var splits = listValueString[0].Split(GiftDataFormatterDefine.kArrayOrListItemTypeSeperator);
                var arrayItemTypeString = splits[splits.Length - 1];
                var arrayItemType = SupportTypes.ConvertIntValueToType(Convert.ToInt32(arrayItemTypeString));
                list = Array.CreateInstance(arrayItemType, listValueString.Count);
            }
            else
            {
                list = (IList)Activator.CreateInstance(valueType);
            }

            for (int index = 0; index < listValueString.Count; index++)
            {
                var listItemString = listValueString[index];
                var listItemStringArray = listItemString.Split(GiftDataFormatterDefine.kArrayOrListItemTypeSeperator);
                var listItemTypeIntString = listItemStringArray[listItemStringArray.Length - 1];
                var listItemTypeInt = Convert.ToInt32(listItemTypeIntString);
                var listItemValueString = listItemString.Substring(0, listItemString.Length - listItemTypeIntString.Length - GiftDataFormatterDefine.kArrayOrListItemTypeSeperator.Length);
                var listItemType = FindArrayOrListItemType(valueType, listItemTypeInt, listItemValueString);
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
        return valueObject;
    }

    private static void SplitArrayOrListString(string s, ref List<string> result)
    {
        int arrayOrListDepth = 0;
        int startIndex = 0;

        if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kArrayOrListStart))
        {
            arrayOrListDepth++;
            startIndex += GiftDataFormatterDefine.kArrayOrListStart.Length;
        }

        while (arrayOrListDepth > 0)
        {
            if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kArrayOrListStart))
            {
                arrayOrListDepth++;
                startIndex += GiftDataFormatterDefine.kArrayOrListStart.Length;
            }
            else if (s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kArrayOrListEnd))
            {
                arrayOrListDepth--;
                startIndex += GiftDataFormatterDefine.kArrayOrListEnd.Length;
            }
            else
            {
                startIndex++;
            }
        }

        while (!s.Substring(startIndex).StartsWith(GiftDataFormatterDefine.kArrayOrListItemSeperator))
        {
            //current 's' is just origin 's' last part.
            if (startIndex >= s.Length)
            {
                break;
            }
            startIndex++;
        }

        if (startIndex > 0)
        {
            var splitPart = s.Substring(0, startIndex);
            var newStringStartIndex = Math.Min(startIndex + GiftDataFormatterDefine.kArrayOrListItemSeperator.Length, s.Length);
            s = s.Substring(newStringStartIndex);
            result.Add(splitPart);
        }

        if (!string.IsNullOrEmpty(s))
        {
            SplitArrayOrListString(s, ref result);
        }
    }

    private static Type FindArrayOrListItemType(Type listType, int listItemTypeInt, string listItemValueString)
    {
        Type listItemType = SupportTypes.ConvertIntValueToType(listItemTypeInt);

        if (listType == SupportTypes.ArrayList.type 
            || listType == SupportTypes.IGenericList.type 
            || listType == SupportTypes.Array.type)
        {
            if (listItemType == SupportTypes.IGenericList.type)
            {
                List<string> listItemListValueString = new List<string>();
                listItemValueString = listItemValueString.Substring(1, listItemValueString.Length - 2);
                SplitArrayOrListString(listItemValueString, ref listItemListValueString);
                var firstListItem = listItemListValueString[0];
                var splits = firstListItem.Split(GiftDataFormatterDefine.kArrayOrListItemTypeSeperator);
                var typeString = splits[splits.Length - 1];
                int typeInt = Convert.ToInt32(typeString);
                var valueString = firstListItem.Substring(0, firstListItem.Length - typeString.Length - GiftDataFormatterDefine.kArrayOrListItemTypeSeperator.Length);
                Type listItemListItemType = FindArrayOrListItemType(listItemType, typeInt, valueString);
                listItemType = listItemType.MakeGenericType(listItemListItemType);
            }
        }
        else
        {
            // https://www.codeproject.com/Tips/5267157/How-to-Get-a-Collection-Element-Type-Using-Reflect
            if (listItemType == SupportTypes.IGenericList.type)
            {
                var etype = typeof(IEnumerable<>);
                foreach (var bt in listType.GetInterfaces())
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
                foreach (var prop in listType.GetProperties())
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
