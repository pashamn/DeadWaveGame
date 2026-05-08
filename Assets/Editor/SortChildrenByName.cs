using UnityEngine;
using UnityEditor;
using System.Linq;

public class SortChildrenByName
{
    [MenuItem("Tools/Sort Selected Object Children By Name")]
    static void SortByName()
    {
        GameObject parent = Selection.activeGameObject;

        if (parent == null)
        {
            Debug.LogWarning("Pilih parent object terlebih dahulu.");
            return;
        }

        var children = parent.transform.Cast<Transform>()
            .OrderBy(t => t.name)
            .ToList();

        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
        }

        Debug.Log("Children berhasil diurutkan berdasarkan nama.");
    }
}