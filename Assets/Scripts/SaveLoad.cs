using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public class SaveLoad<T>
{
    private readonly string ext = ".txt";//확장자

    private FileStream file;

    public void Save(T tempData, string filename, string path)
    {
        string data = JsonUtility.ToJson(tempData);
        string FilePath = path + "/" + filename + ext;

        if(!Directory.Exists(path + "/"))
        {
            Directory.CreateDirectory(path + "/");
        }
        if (!File.Exists(FilePath))
        {
            file = File.Create(FilePath);
            file.Close();
        }
        File.WriteAllText(FilePath, data);
    }

    public bool Load(string filename, string path, out T deserialized)
    {
        string FilePath = path + "/" + filename + ext;
        string json = "";

        if(File.Exists(FilePath))
        {
            //file = File.Open(path + "/" + filename + ext, FileMode.Open);
            json = File.ReadAllText(FilePath);
        }else
        {
            deserialized = default;
            return false;
        }

        deserialized = JsonUtility.FromJson<T>(json);
        return true;

    }
}

public static class SaveLoad
{
    private static string Key_String = "(Nbs,&!^";//8자리 고정

    [System.Serializable]
    class Wrapping<V>
    {
        public Wrapping(V data)
        {
            Data = data;
        }

        public V Data;
    }

    public static void Save<T>(T data, string Path, string FileName, string ext)
    {
        string FilePath = Path + "/" + FileName + "." + ext; 

        if (!Directory.Exists(Path + "/"))
        {
            Directory.CreateDirectory(Path + "/");
        }
        if (!File.Exists(FilePath))
        {
            FileStream file = File.Create(FilePath);
            file.Close();
        }
        File.WriteAllText(FilePath, JsonUtility.ToJson(new Wrapping<T>(data)));

    }//ext는 확장자 . 빼고 입력 / Auto Wrapping
    public static bool Load<T>(string Path, string FileName, string ext, out T deserialized)
    {
        string FilePath = Path + "/" + FileName + "." + ext;
        string json = "";

        if (File.Exists(FilePath))
        {
            //file = File.Open(path + "/" + filename + ext, FileMode.Open);
            json = File.ReadAllText(FilePath);
        }
        else
        {
            deserialized = default;
            return false;
        }

        deserialized = JsonUtility.FromJson<Wrapping<T>>(json).Data;
        return true;
    }//ext는 확장자 . 빼고 입력 / Auto Wrapping

    #region 암호화 호출함수
    static byte[] StringToByte(string value)
    {
        /*
        byte[] buffer = new byte[value.Length];

        for (int i = 0; i < value.Length; i++)
        {
            buffer[i] = System.Convert.ToByte(value.Substring(i, 1));
        }
        *
        return buffer;*/

        byte[] StrByte = System.Text.Encoding.UTF8.GetBytes(value);
        return StrByte;
    }
    static string ByteToString(byte[] strByte)
    {
        return System.Text.Encoding.Default.GetString(strByte);
    }

    public static byte[] Encrypt(string text, System.Security.Cryptography.SymmetricAlgorithm key)
    {
        // Create a memory stream.
        MemoryStream ms = new MemoryStream();

        // Create a CryptoStream using the memory stream and the
        // CSP DES key. 
        CryptoStream encStream = new CryptoStream(ms, key.CreateEncryptor(), CryptoStreamMode.Write);

        // Create a StreamWriter to write a string
        // to the stream.
        StreamWriter sw = new StreamWriter(encStream);

        // Write the plaintext to the stream.
        sw.WriteLine(text);

        // Close the StreamWriter and CryptoStream.
        sw.Close();
        encStream.Close();

        // Get an array of bytes that represents
        // the memory stream.
        byte[] buffer = ms.ToArray();

        // Close the memory stream.
        ms.Close();

        // Return the encrypted byte array.
        return buffer;
    }
    public static string Decrypt(byte[] cypherText, SymmetricAlgorithm key)
    {
        // Create a memory stream to the passed buffer.
        MemoryStream ms = new MemoryStream(cypherText);

        // Create a CryptoStream using the memory stream and the
        // CSP DES key.
        CryptoStream encStream = new CryptoStream(ms, key.CreateDecryptor(), CryptoStreamMode.Read);

        // Create a StreamReader for reading the stream.
        StreamReader sr = new StreamReader(encStream);

        // Read the stream as a string.
        string val = sr.ReadLine();

        // Close the streams.
        sr.Close();
        encStream.Close();
        ms.Close();

        return val;
    }

 
    /// <summary>
    /// 실제 암호화 할 때 호출하는 함수
    /// </summary>
    /// <param name="inputvalue"></param>
    /// <returns></returns>
    public static byte[] Des_Encrypt(string inputvalue)
    {
        //string output;

        // Create a new DES key.
        DESCryptoServiceProvider key = new DESCryptoServiceProvider();

        key.Key = StringToByte(Key_String);
        key.IV = StringToByte(Key_String);

        // Encrypt a string to a byte array.
        byte[] buffer = Encrypt(inputvalue, key);

        return buffer;
    }


    /// <summary>
    /// 복호화를 위한 호출 함수
    /// </summary>
    /// <param name="inputvalue"></param>
    /// <returns></returns>
    public static string Des_Dncrypt(byte[] inputvalue)
    {
        string output;

        // Create a new DES key.
        DESCryptoServiceProvider key = new DESCryptoServiceProvider();

        key.Key = StringToByte(Key_String);
        key.IV = StringToByte(Key_String);

        // Encrypt a string to a byte array.
        output = Decrypt(inputvalue, key);

        return output;
    }
    #endregion

    public static void CryptionSave<T>(T data, string Path, string FileName, string ext)
    {
        string FilePath = Path + "/" + FileName + "." + ext;

        if (!Directory.Exists(Path + "/"))
        {
            Directory.CreateDirectory(Path + "/");
        }
        if (!File.Exists(FilePath))
        {
            FileStream file = File.Create(FilePath);
            file.Close();
        }
        //File.WriteAllText(FilePath, JsonUtility.ToJson(new Wrapping<T>(data)));
        File.WriteAllBytes(FilePath, Des_Encrypt(JsonUtility.ToJson(new Wrapping<T>(data))));

    }//ext는 확장자 . 빼고 입력 / Auto Wrapping
    public static bool CryptionLoad<T>(string Path, string FileName, string ext, out T deserialized)
    {
        string FilePath = Path + "/" + FileName + "." + ext;
        string json = "";

        if (File.Exists(FilePath))
        {
            //file = File.Open(path + "/" + filename + ext, FileMode.Open);
            //json = File.ReadAllText(FilePath);
            json = Des_Dncrypt(File.ReadAllBytes(FilePath));
        }
        else
        {
            deserialized = default;
            return false;
        }

        deserialized = JsonUtility.FromJson<Wrapping<T>>(json).Data;
        return true;
    }//ext는 확장자 . 빼고 입력 / Auto Wrapping
}