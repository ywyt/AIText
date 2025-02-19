using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

public static class EnumExt
{
    /// <summary>
    /// 显示特性上的说明文字
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ExtName(this Enum value)
    {
        try
        {
            FieldInfo field = value.GetType().GetField(value.ToString());

            var va = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as System.ComponentModel.DescriptionAttribute;

            return va == null ? value.ToString() : va.Description;



        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取枚举特性集合
    /// </summary>
    /// <returns></returns>
    public static List<string> GetList(this Enum value)
    {
        List<string> list = new List<string>();
        FieldInfo[] fieldinfo = value.GetType().GetFields();
        foreach (FieldInfo item in fieldinfo)
        {
            Object[] obj = item.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (obj != null && obj.Length != 0)
            {
                DescriptionAttribute des = (DescriptionAttribute)obj[0];
                list.Add(des.Description);
            }
        }
        return list;
    }

    /// <summary>
    /// 获取枚举特性集合
    /// </summary>
    /// <returns></returns>
    public static Dictionary<int, string> GetDictionary(this Enum value)
    {
        Dictionary<int, string> list = new Dictionary<int, string>();
        FieldInfo[] fieldinfo = value.GetType().GetFields(BindingFlags.Static | BindingFlags.Public);

        foreach (FieldInfo item in fieldinfo)
        {
            Object[] obj = item.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (obj != null && obj.Length != 0)
            {
                DescriptionAttribute des = (DescriptionAttribute)obj[0];

                list.Add(item.GetValue(null).GetHashCode(), des.Description);
            }
            else
            {
                list.Add(item.GetValue(null).GetHashCode(), item.Name);
            }
        }
        return list;
    }
}
