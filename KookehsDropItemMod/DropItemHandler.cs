﻿using System;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace DropItems
{
	public class DropItemHandler : MonoBehaviour, IPointerClickHandler
	{
		public Func<ItemIndex> GetItemIndex { get; set; }
		public Func<Inventory> GetInventory { get; set; }
		public bool EquipmentIcon { get; set; }

		public void OnPointerClick(PointerEventData eventData)
		{
			var inventory = GetInventory();

			if (!inventory.hasAuthority)
			{
				return;
			}

			if (!NetworkServer.active)
			{
				// Client, send command
				DropItemMessage itemDropMessage;
				if (EquipmentIcon)
				{
					var equipmentIndex = inventory.GetEquipmentIndex();
					itemDropMessage = new DropItemMessage(equipmentIndex);
				}
				else
				{
					var itemIndex = GetItemIndex();
					itemDropMessage = new DropItemMessage(itemIndex);
				}

				KookehsDropItemMod.DropItemCommand.Invoke(itemDropMessage);
			} 
			else
			{
				// Server, execute command
				var characterBody = inventory.GetComponent<CharacterMaster>().GetBody();
				var charTransform = characterBody.GetFieldValue<Transform>("transform");

				var pickupIndex = EquipmentIcon 
					? new PickupIndex(inventory.GetEquipmentIndex()) 
					: new PickupIndex(GetItemIndex());

				DropItem(charTransform, inventory, pickupIndex);
				CreateNotification(characterBody, charTransform, pickupIndex);
			}
		}

		public static void DropItem(Transform charTransform, Inventory inventory, PickupIndex pickupIndex)
		{
			if (pickupIndex.equipmentIndex != EquipmentIndex.None)
			{
				if (inventory.GetEquipmentIndex() != pickupIndex.equipmentIndex)
				{
					return;
				}

				inventory.SetEquipmentIndex(EquipmentIndex.None);
			}
			else
			{
				if (inventory.GetItemCount(pickupIndex.itemIndex) <= 0) 
				{
					return;
				}

				inventory.RemoveItem(pickupIndex.itemIndex, 1);
			}

			PickupDropletController.CreatePickupDroplet(pickupIndex,
				charTransform.position, Vector3.up * 20f + charTransform.forward * 10f);
		}

		public static void CreateNotification(CharacterBody character, Transform transform, PickupIndex pickupIndex)
		{
			if (pickupIndex.equipmentIndex != EquipmentIndex.None)
			{
				CreateNotification(character, transform, pickupIndex.equipmentIndex);
			} 
			else
			{
				CreateNotification(character, transform, pickupIndex.itemIndex);
			}
		}

		private static void CreateNotification(CharacterBody character, Transform transform, EquipmentIndex equipmentIndex)
		{
			var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
			const string title = "Equipment dropped";
			var description = Language.GetString(equipmentDef.nameToken);
			var texture = Resources.Load<Texture>(equipmentDef.pickupIconPath);

			CreateNotification(character, transform, title, description, texture);
		}

		private static void CreateNotification(CharacterBody character, Transform transform, ItemIndex itemIndex)
		{
			var itemDef = ItemCatalog.GetItemDef(itemIndex);
			const string title = "Item dropped";
			var description = Language.GetString(itemDef.nameToken);
			var texture = Resources.Load<Texture>(itemDef.pickupIconPath);

			CreateNotification(character, transform, title, description, texture);
		}

		private static void CreateNotification(CharacterBody character, Transform transform, string title, string description, Texture texture)
		{
			var notification = character.gameObject.AddComponent<Notification>();
			notification.transform.SetParent(transform);
			notification.SetPosition(new Vector3((float)(Screen.width * 0.8), (float)(Screen.height * 0.25), 0));
			notification.SetIcon(texture);
			notification.GetTitle = () => title;
			notification.GetDescription = () => description;
		}
	}
}