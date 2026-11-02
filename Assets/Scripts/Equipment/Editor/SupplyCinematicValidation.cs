using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ArmySurvivor.Equipment.Editor
{
    public static class SupplyCinematicValidation
    {
        [MenuItem("Army Survivor/Equipment/Bind Door Pivots")]
        public static void Bind()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene=SceneManager.GetActiveScene();
            if(scene.name!="VillageScene")throw new InvalidOperationException("Open VillageScene first.");
            Transform a=null,b=null,gate=null;
            foreach(var root in scene.GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if(t.name=="Wall_A_gate")gate=t;
                    if(t.name=="Wall_A_door_A" && t.parent!=null && t.parent.name.StartsWith("DoorPivot"))a=t.parent;
                    if(t.name=="Wall_A_door_B" && t.parent!=null && t.parent.name.StartsWith("DoorPivot"))b=t.parent;
                }
            if(!a||!b||!gate||a==b)throw new InvalidOperationException("Two separate DoorPivot parents and Wall_A_gate are required.");
            var controller=gate.GetComponent<SupplySlidingGate>();
            if(!controller)controller=Undo.AddComponent<SupplySlidingGate>(gate.gameObject);
            Undo.RecordObject(controller,"Bind authored door pivots");
            controller.doorA=a;controller.doorB=b;
            foreach(var pivot in new[]{a,b})
            {
                foreach(var old in pivot.GetComponentsInChildren<Animation>(true)) {Undo.RecordObject(old,"Disable leaf animation");old.enabled=false;old.playAutomatically=false;}
                foreach(var old in pivot.GetComponentsInChildren<Animator>(true)) {Undo.RecordObject(old,"Disable leaf animator");old.enabled=false;}
            }
            foreach(var old in gate.GetComponentsInChildren<Animator>(true)) {Undo.RecordObject(old,"Disable gate animator");old.enabled=false;}
            controller.openA=MakeClip(a,controller.openAngleA,"Assets/EquipmentData/Wall_A_door_A-RotateOpen.anim");
            controller.openB=MakeClip(b,controller.openAngleB,"Assets/EquipmentData/Wall_A_door_B-RotateOpen.anim");
            EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);
            Validate();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        }
        static AnimationClip MakeClip(Transform target,float angle,string path)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.name=System.IO.Path.GetFileNameWithoutExtension(path);
            clip.ClearCurves();clip.legacy=true;var e=target.localEulerAngles;
            clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.x",AnimationCurve.Constant(0,1,e.x));
            clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.y",AnimationCurve.EaseInOut(0,e.y,1,e.y+angle));
            clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.z",AnimationCurve.Constant(0,1,e.z));
            EditorUtility.SetDirty(clip);return clip;
        }
        [MenuItem("Army Survivor/Equipment/Validate Supply Cinematic")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SupplySlidingGate g=null;
            foreach(var candidate in UnityEngine.Object.FindObjectsByType<SupplySlidingGate>(FindObjectsSortMode.None)) if(candidate.Ready) {g=candidate;break;}
            if(!g||!g.Ready)throw new InvalidOperationException("Bind Door Pivots first.");
            var a=g.doorA;var b=g.doorB;var pa=a.localPosition;var pb=b.localPosition;
            var ra=a.localRotation;var rb=b.localRotation;
            try
            {
                g.Sample(1);
                if((a.localPosition-pa).sqrMagnitude>.000001f || (b.localPosition-pb).sqrMagnitude>.000001f ||
                    Quaternion.Angle(a.localRotation,ra*Quaternion.AngleAxis(g.openAngleA,Vector3.up))>.01f ||
                    Quaternion.Angle(b.localRotation,rb*Quaternion.AngleAxis(g.openAngleB,Vector3.up))>.01f)
                    throw new InvalidOperationException("Hinge animation must rotate only local Y without translating.");
                Debug.Log("SUPPLY_PIVOTS_VALIDATED: authored positions preserved; A/B local Y rotation verified.");
            }
            finally {a.localPosition=pa;b.localPosition=pb;a.localRotation=ra;b.localRotation=rb;}
        }
    }
}
