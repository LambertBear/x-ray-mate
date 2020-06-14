using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
// using MaterialOptionsWrapper;

namespace MVRPlugin {

	public class XRayMate : MVRScript {

        public const int BASE_QUEUE = 3000;
		public const int MAX_QUEUE_OFFSET = 6;
        public const int DEFAULT_CLOTHING_LAYER = 2;
		// I have to go to programming jail
		public List<string> AVAIL_LAYERS = new List<string>(new string[] {"-", "0", "1", "2", "3"});

        public JSONStorableFloat power;
		public JSONStorableAction refresh_list;
		public JSONStorableAction refresh_materials;
		public List<Atom> people_list = new List<Atom>();
		public List<TransparentClothingItem> transparent_clothing_list = new List<TransparentClothingItem>();
        protected Material xray_mat;
		protected int queue_previous = 0;

		public string[] tags_L0 = {"Accessory", "Jewelry", "Stockings", "Socks", "Gloves", "Hat", "Glasses"};
		public string[] tags_L1 = {"Bra", "Underwear", "Panties", "Lingerie"};
		public string[] tags_L2 = {};
		public string[] tags_L3 = {"Dress", "Sweater", "Skirt"};

		public struct TransparentClothingItem{
			public JSONStorableStringChooser layer;
			public DAZSkinWrapMaterialOptions material;
		}

        public override void Init() {
			try {
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

				refresh_list = new JSONStorableAction("Reset clothing list", UpdateClothingList);
				RegisterAction(refresh_list);
				refresh_materials = new JSONStorableAction("Apply clothing layers", UpdateMaterials);
				RegisterAction(refresh_materials);

				var updateMaterialButton = CreateButton("Apply clothing layers", false);
            	updateMaterialButton.button.onClick.AddListener(() =>
				{
					UpdateMaterials();
				});
				
				var updateClothingButton = CreateButton("Reset clothing list", false);
				updateClothingButton.buttonColor = new Color(1.0f, 0.4f, 0.4f);
            	updateClothingButton.button.onClick.AddListener(() =>
				{
					UpdateClothingList();
				});

				UpdateClothingList();
				UpdateMaterials();

				//SuperController.LogMessage(init_msg);
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		void UpdateMaterials() {

			foreach(var c in transparent_clothing_list){
				int queue;

				if(c.layer.val == "-"){
					queue = BASE_QUEUE - 1;
				} else {
					queue = BASE_QUEUE + 2 * int.Parse(c.layer.val) + 1;
				}

				foreach(Material mat in c.material.skinWrap.GPUmaterials){
					mat.renderQueue = queue;
				}
				
				if(c.material.skinWrap2 != null){
					foreach(var mat in c.material.skinWrap2.GPUmaterials){
						mat.renderQueue = queue;
					}
				}

			}

		}

		void UpdateClothingList() {

			string init_msg = "";
			people_list = new List<Atom>();
			foreach(Atom a in SuperController.singleton.GetAtoms()){
				if( a.category == "People"){
					people_list.Add(a);
					init_msg += string.Format("Found the person {0} \n", a.name);
				}
			}

			foreach(var c in transparent_clothing_list){
				RemovePopup(c.layer);
			}
			transparent_clothing_list = new List<TransparentClothingItem>(0);

			foreach(Atom person in people_list){
				DAZSkinWrapMaterialOptions[] clothes_materials = person.GetComponentsInChildren<DAZSkinWrapMaterialOptions>(false);
				DAZClothingItemControl[] clothes_items = person.GetComponentsInChildren<DAZClothingItemControl>(false);

				init_msg += clothes_materials.Length.ToString() + " clothes materials found\n";
				init_msg += "current xray queue " + queue_previous.ToString();

				foreach(DAZSkinWrapMaterialOptions c in clothes_materials){
					List<string> tags = new List<string>(new string[] {});
					init_msg += "\n" + c.storeId + "\n    ";

					if(c.storeId.Contains("Scalp")){
						int queue = BASE_QUEUE - 1;

						foreach(Material mat in c.skinWrap.GPUmaterials){
							mat.renderQueue = queue;
						}
						if(c.skinWrap2 != null){
							foreach(var mat in c.skinWrap2.GPUmaterials){
								mat.renderQueue = queue;
							}
						}
						continue;
					} 

					// Try to find the matching ClothingItemControl
					foreach(DAZClothingItemControl ci in clothes_items){
						if(c.storeId.Contains(ci.storeId.Replace("ItemControl", ""))){
							tags = new List<string>(ci.clothingItem.tagsArray);
						}
					}
					string default_layer = LayerFromTags(tags).ToString();
					init_msg += "Default layer: " + default_layer + "\n";

					var tr_item = new TransparentClothingItem();
					tr_item.material = c;
					tr_item.layer = new JSONStorableStringChooser(c.storeId + "_layer", AVAIL_LAYERS, default_layer, c.storeId);
					UIDynamicPopup udp = CreateScrollablePopup(tr_item.layer, true);
					udp.labelWidth = 300.0f;
					transparent_clothing_list.Add(tr_item);
				}
			}
			//SuperController.LogMessage(init_msg);
			UpdateMaterials();
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