using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SceneNotes.Editor
{
    public static class ContextualMenu
    {
        public static void BuildRightClickMenu(ContextualMenuPopulateEvent evt,
            bool copyPropertyPath = true, bool searchSamePropValue = true, bool findReferencesTo = true,
            bool copy = true, bool copyPath = true, bool copyGuid = true, bool paste = true)
        {
            DropdownMenu menu = evt.menu;
            ObjectField objectField = evt.target as ObjectField;
            Object currentValue = objectField?.value;

            int i = -1;

            if (copyPropertyPath)
            {
                i++;
                menu.InsertAction(i, "Copy Property Path", action =>
                {
                    string propertyPath = GetPropertyPath(objectField);
                    EditorGUIUtility.systemCopyBuffer = propertyPath;
                });
            }

            if (searchSamePropValue)
            {
                i++;
                menu.InsertAction(i,"Search Same Property Value", action =>
                {
                    if (currentValue != null)
                    {
                        // Open search window filtered by this asset type and value
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = currentValue;
                        EditorGUIUtility.PingObject(currentValue);
                    }
                }, currentValue != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            bool findReferencesToShown = false;
            if (findReferencesTo && currentValue != null)
            {
                findReferencesToShown = true;
                i++;
                menu.InsertAction(i,$"Find references to {currentValue.name}", action =>
                {
                    if (currentValue != null)
                    {
                        Selection.activeObject = currentValue;
                        EditorUtility.FocusProjectWindow();

                        // This opens the reference search (equivalent to right-click > Find References in Project)
                        MethodInfo method = typeof(EditorUtility).GetMethod("FindReferences",
                            BindingFlags.NonPublic | BindingFlags.Static);
                        method?.Invoke(null, new object[] { currentValue });
                    }
                }, currentValue != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if (copyPropertyPath || searchSamePropValue || findReferencesToShown)
            {
                i++;
                menu.InsertSeparator("", i);
            }

            if(copy)
            {
                i++;
                menu.InsertAction(i, "Copy", action =>
                {
                    if (currentValue == null) return;
                    
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(currentValue, out string guidFound,
                        out long localIdFound);
                    EditorGUIUtility.systemCopyBuffer = "UnityEditor.ObjectWrapperJSON:" + JsonUtility.ToJson(
                        new ClipboardData
                        {
                            instanceID = currentValue.GetInstanceID(),
                            guid = guidFound,
                            localID = localIdFound,
                            type = currentValue.GetType().AssemblyQualifiedName
                        });
                }, currentValue != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if(copyPath)
            {
                i++;
                menu.InsertAction(i, "Copy Path", action =>
                {
                    if (currentValue != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(currentValue);
                        EditorGUIUtility.systemCopyBuffer = assetPath;
                    }
                }, currentValue != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if(copyGuid)
            {
                i++;
                menu.InsertAction(i, "Copy GUID", action =>
                {
                    if (currentValue != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(currentValue);
                        string guid = AssetDatabase.AssetPathToGUID(assetPath);
                        EditorGUIUtility.systemCopyBuffer = guid;
                    }
                }, currentValue != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if(paste)
            {
                i++;
                menu.InsertAction(i, "Paste", action =>
                {
                    // Try to deserialize from clipboard and set the object
                    try
                    {
                        string clipboardText = EditorGUIUtility.systemCopyBuffer;
                        string json = clipboardText.Substring(clipboardText.IndexOf('{'));
                        ClipboardData clipboardData = JsonUtility.FromJson<ClipboardData>(json);
                        Object obj = EditorUtility.InstanceIDToObject(clipboardData.instanceID);
                        if (obj != null && IsValidObjectForField(obj, objectField)) objectField.value = obj;
                    }
                    catch
                    {
                        // Clipboard doesn't contain valid object data
                    }
                }, CanPasteObject() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if (copy || copyGuid || copyPath || paste)
            {
                i++;
                menu.InsertSeparator("", i);
            }
        }

        private static string GetPropertyPath(ObjectField objectField)
        {
            // This is a simplified version - we might need to traverse up the property hierarchy
            return objectField.bindingPath ?? objectField.name;
        }

        private static bool IsValidObjectForField(Object obj, ObjectField field)
        {
            Type fieldType = field.objectType;
            return fieldType == null || fieldType.IsAssignableFrom(obj.GetType());
        }

        private static bool CanPasteObject()
        {
            try
            {
                string clipboardText = EditorGUIUtility.systemCopyBuffer;
                int startIndex = clipboardText.IndexOf('{');
                if (startIndex <= 0) return false;
                string json = clipboardText.Substring(startIndex);
                ClipboardData clipboardData = JsonUtility.FromJson<ClipboardData>(json);
                Object obj = EditorUtility.InstanceIDToObject(clipboardData.instanceID);
                return obj != null;
            }
            catch  (Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
        }

        [Serializable]
        private class ClipboardData
        {
            public int instanceID;
            public long localID;
            public string type;
            public string guid;
        }
    }
}