using UnityEngine;

namespace ArmySurvivor.Equipment
{
    public sealed class SupplyUIFit : MonoBehaviour
    {
        void OnEnable() => Fit();
        void OnRectTransformDimensionsChange() => Fit();
        void Fit()
        {
            var parent=transform.parent as RectTransform;
            if(parent==null)return;
            float s=Mathf.Min(parent.rect.width/1920f,parent.rect.height/1080f);
            transform.localScale=Vector3.one*Mathf.Max(.01f,s);
        }
    }
}
