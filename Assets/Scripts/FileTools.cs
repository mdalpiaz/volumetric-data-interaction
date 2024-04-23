using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Constants;
using Newtonsoft.Json;
using UnityEngine;

public static class FileTools
{
    public static string SaveBitmapPng(Texture2D image)
    {
        var fileLocation = GetDatedFilePath();
        File.WriteAllBytes($"{fileLocation}.png", image.EncodeToPNG());
        //File.WriteAllBytes($"{fileLocation}.bmp", image.EncodeToBMP());
        return fileLocation;
    }

    private static string GetDatedFilePath(string name = "plane", string path = StringConstants.ImagesFolderPath)
    {
        var fileName = DateTime.Now.ToString("yy-MM-dd hh.mm.ss " + name);
        EnsurePathExists(path);
        return Path.Combine(path, fileName);
    }
        
    private static void EnsurePathExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
        
    /// <summary>
    /// The Unity ImageConversion API is used, which supports loading of .png and .jpg files.
    /// </summary>
    /// <param name="path">The path the image is located at.</param>
    /// <returns>A Texture2D object with the loaded image. Invalid images still return valid Texture2D objects, so it needs to be checked.</returns>
    public static Texture2D LoadImage(string path)
    {
        var texture = new Texture2D(2, 2)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.LoadImage(File.ReadAllBytes(path));
        return texture;
    }

    public static void SaveScreenPositions(IEnumerable<Vector3> positions)
    {
        var vectors = positions.Select(Vector3Simple.FromVector3).ToArray();
        var json = JsonConvert.SerializeObject(vectors);
        var path = Path.Join(Application.persistentDataPath, "/screens.dat");

        var serializer = new JsonSerializer();
        using var streamWriter = new StreamWriter(path, false, Encoding.UTF8);
        using var jsonWriter = new JsonTextWriter(streamWriter);
        serializer.Serialize(jsonWriter, vectors);
    }

    public static bool LoadScreenPositions(out Vector3[] positions)
    {
        var path = Path.Join(Application.persistentDataPath, "/screens.dat");

        try
        {
            var serializer = new JsonSerializer();
            using var file = new FileStream(path, FileMode.Open);
            using var streamReader = new StreamReader(file);
            using var jsonReader = new JsonTextReader(streamReader);
            var simplePositions = serializer.Deserialize<Vector3Simple[]>(jsonReader);
            positions = simplePositions.Select(Vector3Simple.ToVector3).ToArray();
            return true;
        }
        catch
        {
            positions = null;
            return false;
        }
    }
}
