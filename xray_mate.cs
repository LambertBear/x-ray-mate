using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace MVRPlugin {
	public class XRayMate : MVRScript {

        private const int BASE_QUEUE = 2410;
		private const int MAX_QUEUE_OFFSET = 6;
        private const int DEFAULT_CLOTHING_LAYER = 2;
        public JSONStorableFloat power;
		public JSONStorableAction refresh;
		public List<Atom> people_list = new List<Atom>();
        protected Material xray_mat;
		protected int queue_previous = 0;

		public string[] tags_L0 = {"Accessory", "Jewelry", "Stockings", "Socks", "Gloves", "Hat", "Glasses"};
		public string[] tags_L1 = {"Bra", "Underwear", "Panties", "Lingerie"};
		public string[] tags_L2 = {};
		public string[] tags_L3 = {"Dress", "Sweater", "Skirt"};

        public override void Init() {
			try {
                string init_msg = "";
                Renderer rend = containingAtom.GetComponentInChildren<Renderer>(false);
				rend.shadowCastingMode = 0;
				rend.receiveShadows = false;
                xray_mat = rend.material;
                xray_mat.renderQueue = BASE_QUEUE;
				xray_mat.SetFloat("_AlphaAdjust", -0.6f);
				xray_mat.SetColor("_Color", new Color(0f, 0f, 0f));

                power = new JSONStorableFloat("Power", 0, 0, 100, true, true);
                RegisterFloat(power);
                CreateSlider(power);

				refresh = new JSONStorableAction("Update clothing", UpdateClothing);
				RegisterAction(refresh);
				UpdateClothing();

				//SuperController.LogMessage(init_msg);
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		void UpdateClothing() {

			string init_msg = "";
			people_list = new List<Atom>();
			foreach(Atom a in SuperController.singleton.GetAtoms()){
				if( a.category == "People"){
					people_list.Add(a);
					init_msg += string.Format("Found the person {0} \n", a.name);
				}
			}

			foreach(Atom person in people_list){
				var hair_controls = person.GetComponentsInChildren<HairSimControl>(false);
				init_msg += hair_controls.Length.ToString() + " hair objects found\n";
				foreach(var h in hair_controls){
					var hair_rend = h.hairSettings.RenderSettings;
					//hair_rend.material = new Material(hair_rend.material);
					hair_rend.material.renderQueue = BASE_QUEUE - 1;
					hair_rend.IsVisible = false;
				}
			}

			foreach(Atom person in people_list){
				DAZSkinWrapMaterialOptions[] clothes_materials = person.GetComponentsInChildren<DAZSkinWrapMaterialOptions>(false);
				DAZClothingItemControl[] clothes_items = person.GetComponentsInChildren<DAZClothingItemControl>(false);

				init_msg += clothes_materials.Length.ToString() + " clothes materials found\n";
				init_msg += "current xray queue " + queue_previous.ToString();

				foreach(DAZSkinWrapMaterialOptions c in clothes_materials){
					List<string> tags = new List<string>(new string[] {});
					init_msg += "\n" + c.storeId + "\n    ";

					foreach(DAZClothingItemControl ci in clothes_items){
						// init_msg += ", " + ci.storeId;
						if(c.storeId.Contains(ci.storeId.Replace("ItemControl", ""))){
							tags = new List<string>(ci.clothingItem.tagsArray);
						}
					}
					foreach(var si in tags){
						init_msg += si.ToString() + ", ";
					}

					int queue = BASE_QUEUE + 2 * LayerFromTags(tags) + 1;
					if(c.storeId.Contains("Scalp")){
						queue = BASE_QUEUE - 1;
					} 
					init_msg += "\n" + queue.ToString() + "\n";

					foreach(var mat in c.skinWrap.GPUmaterials){
						mat.renderQueue = queue;
					}
					
					if(c.skinWrap2 != null){
						foreach(var mat in c.skinWrap2.GPUmaterials){
							mat.renderQueue = queue;
						}
					}
				}
				SuperController.LogMessage(init_msg);
			}
			//SuperController.LogMessage(init_msg);
		}

		int LayerFromTags(List<string> tags) {
			foreach(string c in tags_L0){
				if(tags.Contains(c)){return 0;}
			}
			foreach(string c in tags_L1){
				if(tags.Contains(c)){return 1;}
			}
			foreach(string c in tags_L2){
				if(tags.Contains(c)){return 2;}
			}
			foreach(string c in tags_L3){
				if(tags.Contains(c)){return 3;}
			}
			return DEFAULT_CLOTHING_LAYER;
		}

		void Start() {
			try {
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		void Update() {
			try {
                int queue_changed = BASE_QUEUE + 2 * MAX_QUEUE_OFFSET 
											   - 2 * Convert.ToInt32(Math.Ceiling(power.val * MAX_QUEUE_OFFSET / 100.0f));

				// don't necessarily want to change the material every frame when not neccessary
				// It might not make a big difference, but it feels like it should be done this way
				if (queue_changed != queue_previous){
					xray_mat.renderQueue = queue_changed;
					queue_previous = queue_changed;
				}
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		// FixedUpdate is called with each physics simulation frame by Unity
		void FixedUpdate() {
			try {
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		void OnDestroy() {
		}

	}
}