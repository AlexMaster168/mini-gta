using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace SunsetState
{
    public sealed class UnsupportedSaveVersionException:IOException
    {public UnsupportedSaveVersionException():base("Сохранение создано более новой версией игры. Файл оставлен без изменений."){} }
    public static class SaveStore
    {
        public const int CurrentVersion=1;
        const int MaximumBytes=2*1024*1024;
        public static void AtomicWrite(string path,string text,bool backup)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary=path+".tmp";byte[] bytes=new UTF8Encoding(false).GetBytes(text);
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            if(File.Exists(path))File.Replace(temporary,path,backup?path+".bak":null);else File.Move(temporary,path);
        }
        static string Digest(SaveData data)
        {string integrity=data.Integrity;data.Integrity=null;string json=JsonUtility.ToJson(data);data.Integrity=integrity;using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-","");}
        public static SaveData Parse(string json)
        {
            if(string.IsNullOrEmpty(json)||json.Length>MaximumBytes||!json.TrimStart().StartsWith("{")||!json.Contains("\"Role\"")||!json.Contains("\"Health\""))throw new InvalidDataException("Неполное сохранение.");
            var data=JsonUtility.FromJson<SaveData>(json);if(data==null)throw new InvalidDataException("Неверное сохранение.");
            if(data.SaveVersion>CurrentVersion)throw new UnsupportedSaveVersionException();
            if(data.SaveVersion<0)throw new InvalidDataException("Неверная версия сохранения.");
            if(data.SaveVersion>0&&(string.IsNullOrEmpty(data.Integrity)||data.Integrity!=Digest(data)))throw new InvalidDataException("Контрольная сумма сохранения не совпадает.");
            foreach(float value in new[]{data.X,data.Z,data.Health,data.Armor,data.Hour,data.Visa})if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidDataException("Неверные числовые данные сохранения.");
            foreach(var array in new[]{data.Purchases,data.LoadedAmmo,data.ReserveAmmo,data.Stories,data.Choices,data.Career,data.Skins,data.Accessories,data.Shields,data.Courts})if(array!=null&&array.Length>4096)throw new InvalidDataException("Слишком большой массив сохранения.");
            return data;
        }
        static SaveData ReadOne(string path)
        {if(!File.Exists(path))throw new FileNotFoundException("Сохранение не найдено.");if(new FileInfo(path).Length>MaximumBytes)throw new InvalidDataException("Сохранение слишком велико.");return Parse(File.ReadAllText(path));}
        public static SaveData Read(string path,out bool recovered)
        {
            recovered=false;
            try{return ReadOne(path);}catch(UnsupportedSaveVersionException){throw;}catch(InvalidDataException){}catch(IOException){}catch(ArgumentException){}
            var data=ReadOne(path+".bak");recovered=true;return data;
        }
        public static void Write(string path,SaveData data)
        {
            bool validPrevious=false;
            if(File.Exists(path)){try{ReadOne(path);validPrevious=true;}catch(UnsupportedSaveVersionException){throw;}catch(InvalidDataException){}catch(IOException){}catch(ArgumentException){}}
            data.SaveVersion=CurrentVersion;data.Integrity=null;data.Integrity=Digest(data);string json=JsonUtility.ToJson(data);Parse(json);if(File.Exists(path)&&!validPrevious)File.Copy(path,path+".corrupt",true);AtomicWrite(path,json,validPrevious);
        }
    }
}
