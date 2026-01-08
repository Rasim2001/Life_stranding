using Infastructure.CutScenes.Custom.Markers;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace Infastructure.CutScenes.Editor
{
    [CustomTimelineEditor(typeof(TransformMarker))]
    public class TransformMarkerEditor : MarkerEditor
    {
        public override void OnCreate(IMarker marker, IMarker clonedFrom) =>
            (marker as TransformMarker)?.RegenerateBindingKey();
    }
}