using System;
using System.Reflection;

using Object = UnityEngine.Object;

/// <summary>
/// Reference Article http://www.codeproject.com/KB/tips/SerializedObjectCloner.aspx
/// Provides a method for performing a deep copy of an object.
/// Binary Serialization is used to perform the copy.
/// </summary>
public static class ObjectCopier
{
    public static object DeepCopy(object obj)
    {
        if (obj == null)
        {
            return null;
        }

        Type type = obj.GetType();

        if (type.IsValueType || type == typeof(string))
        {
            return obj;
        }
        else if (type.IsArray)
        {
            Type elementType = Type.GetType(type.FullName.Replace("[]", string.Empty));
            var array = obj as Array;
            Array copied = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                copied.SetValue(DeepCopy(array.GetValue(i)), i);
            }

            return Convert.ChangeType(copied, obj.GetType());        
        }
        else if (type.IsClass)
        {
            FieldInfo[] fieldArray = type.GetFields();

            object newobj = Activator.CreateInstance(type);

            for (int i = 0; i < fieldArray.Length; i++)
            {
                FieldInfo finfo = fieldArray[i];
                finfo.SetValue(newobj, finfo.GetValue(obj));
            }

            return newobj;
        }

        return null;
    }
}

//public static object DeepCopy(object obj)
//{
//    if (obj == null)
//        return null;
//    Type type = obj.GetType();

//    if (type.IsValueType || type == typeof(string))
//    {
//        return obj;
//    }
//    else if (type.IsArray)
//    {
//        Type elementType = Type.GetType(
//                type.FullName.Replace("[]", string.Empty));
//        var array = obj as Array;
//        Array copied = Array.CreateInstance(elementType, array.Length);
//        for (int i = 0; i < array.Length; i++)
//        {
//            copied.SetValue(DeepCopy(array.GetValue(i)), i);
//        }
//        return Convert.ChangeType(copied, obj.GetType());
//    }
//    else if (type.IsClass)
//    {

//        object toret = Activator.CreateInstance(obj.GetType());
//        FieldInfo[] fields = type.GetFields(BindingFlags.Public |
//                    BindingFlags.NonPublic | BindingFlags.Instance);
//        foreach (FieldInfo field in fields)
//        {
//            object fieldValue = field.GetValue(obj);
//            if (fieldValue == null)
//                continue;
//            field.SetValue(toret, DeepCopy(fieldValue));
//        }
//        return toret;
//    }
//    else
//        throw new ArgumentException("Unknown type");
//}