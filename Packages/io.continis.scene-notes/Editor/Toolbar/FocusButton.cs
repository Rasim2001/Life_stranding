using System.Collections.Generic;
using System.IO;
using SceneNotes.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneNotes.Toolbar
{
    public class FocusButtonMemory : ScriptableSingleton<FocusButtonMemory>
    {
        public List<bool> iconStates;
        public List<bool> gizmoStates;
    }
    
    [EditorToolbarElement(ID, typeof(SceneView))]
    public sealed class FocusButton : EditorToolbarToggle
    {
        public const string ID = "SceneNotes.FocusButton";
        
        public FocusButton()
        {
            name = ID;
            tooltip = "Focus mode. Displays only icons for Scene Notes, hiding icons for all other scripts.";
            
            bool isOn = EditingSession.IsFocusOn();
            SetValueWithoutNotify(isOn);
            EditorApplication.delayCall += () => LoadIcon(isOn);
            this.RegisterValueChangedCallback(OnValueChanged);
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            bool focusOn = evt.newValue;
            if (focusOn)
            {
                FocusButtonMemory.instance.iconStates = new List<bool>();
                FocusButtonMemory.instance.gizmoStates = new List<bool>();
            }
            
            SetFocus(focusOn);
        }

        public void SetFocus(bool on)
        {
            GizmoInfo[] info = GizmoUtility.GetGizmoInfo();
            int i = 0;
            foreach (GizmoInfo gizmoInfo in info)
            {
                if(!gizmoInfo.hasIcon && !gizmoInfo.hasGizmo) continue;
                
                if (on)
                {
                    // Store state and turn off
                    FocusButtonMemory.instance.iconStates.Add(gizmoInfo.iconEnabled);
                    if(gizmoInfo.hasIcon) gizmoInfo.iconEnabled = gizmoInfo.name == nameof(SceneNoteBehaviour);
                
                    FocusButtonMemory.instance.gizmoStates.Add(gizmoInfo.gizmoEnabled);
                    if(gizmoInfo.hasGizmo) gizmoInfo.gizmoEnabled  = gizmoInfo.name == nameof(SceneNoteBehaviour);
                }
                else
                {
                    // retrieve state
                    if(gizmoInfo.hasIcon) gizmoInfo.iconEnabled = FocusButtonMemory.instance.iconStates[i];
                    if(gizmoInfo.hasGizmo) gizmoInfo.gizmoEnabled = FocusButtonMemory.instance.gizmoStates[i];
                }
                
                GizmoUtility.ApplyGizmoInfo(gizmoInfo, false);
                i++;
            }
            
            EditingSession.SetIsFocusing(on);
            SetValueWithoutNotify(on);
            LoadIcon(on);
        }

        private void LoadIcon(bool on)
        {
            string iconEnding = on ? "On" :"Off";
            string iconPath = Path.Combine(Constants.packageAssetsFolder, Constants.uiImagesFolder, Constants.toolbarIconsFolder,
                $"{Constants.imagePrefix}Focus{iconEnding}.png");
            icon = (Texture2D)AssetDatabase.LoadAssetAtPath(iconPath, typeof(Texture2D));
        }

        // private void TurnAll(bool on)
        // {
        //     GizmoInfo[] info = GizmoUtility.GetGizmoInfo();
        //     foreach (GizmoInfo gizmoInfo in info)
        //     {
        //         gizmoInfo.iconEnabled = on;
        //         gizmoInfo.gizmoEnabled = on;
        //         GizmoUtility.ApplyGizmoInfo(gizmoInfo, false);
        //     }
        // }
    }
}