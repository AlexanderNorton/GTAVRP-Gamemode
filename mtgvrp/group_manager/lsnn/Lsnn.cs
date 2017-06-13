﻿using GTANetworkServer;
using GTANetworkShared;
using System.Collections.Generic;
using RoleplayServer.core;
using RoleplayServer.door_manager;
using RoleplayServer.player_manager;
using RoleplayServer.vehicle_manager;
using RoleplayServer.inventory;
using System;
using System.Timers;

namespace RoleplayServer.group_manager.lsnn
{
    class Lsnn : Script
    {
        public Lsnn()
        {
            API.onResourceStart += StartLsnn;
        }

        public readonly Vector3 LsnnFrontDoor = new Vector3(-319.0662f, -609.8559f, 33.55819f);

        public void StartLsnn()
        {
            LsnnFrontDoorShape = API.createCylinderColShape(LsnnFrontDoor, 2f, 3f);

            API.createMarker(1, LsnnFrontDoor - new Vector3(0, 0, 1f), new Vector3(), new Vector3(),
                new Vector3(1f, 1f, 1f), 100, 51, 153, 255);
        }

        public ColShape LsnnFrontDoorShape;
        public string Headline = "Los Santos News Network";
        public bool IsBroadcasting = false;
        public bool CameraSet = false;
        public bool ChopperCamToggle = false;
        public NetHandle Chopper;
        public Vector3 CameraPosition = null;
        public Vector3 CameraRotation = null;
        public Vector3 OffSet = new Vector3(0, 0, -3);
        public Timer ChopperRotation = new Timer();

        [Command("broadcast")]
        public void broadcast_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            if (CameraSet == false)
            {
                API.sendChatMessageToPlayer(player, "You must place a camera before starting a broadcast.");
                return;
            }

            if (IsBroadcasting == true)
            {
                IsBroadcasting = false;
                API.sendChatMessageToPlayer(player, "~p~The broadcast has been stopped.");

                foreach (var c in API.getAllPlayers())
                {
                    Character receivercharacter = API.getEntityData(c, "Character");
                    if (receivercharacter.IsWatchingBroadcast)
                    {

                        API.triggerClientEvent(c, "unwatch_broadcast");
                        receivercharacter.IsWatchingBroadcast = false;
                        API.sendChatMessageToPlayer(player, "~p~" + character.CharacterName + " has stopped the broadcast.");
                    }
                    
                    if (receivercharacter.HasMic == true && receivercharacter.Group.CommandType != Group.CommandTypeLsnn)
                    {
                        receivercharacter.HasMic = false;
                        API.setEntityData(c, "MicStatus", false);
                    }
                }
                return;
            }
            API.sendChatMessageToPlayer(player, "Broadcast started.");
            API.sendChatMessageToAll("~p~" + character.CharacterName + " has started a broadcast. /watchbroadcast to tune in!");
            IsBroadcasting = true;

        }

        [Command("editheadline", GreedyArg = false)]
        public void editbanner_cmd(Client player, string text)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            Headline = text;
            API.sendChatMessageToPlayer(player, "Headline edited.");
        }

        [Command("setcamera")]
        public void setcamera_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            if (character.HasCamera == false)
            {
                API.sendChatMessageToPlayer(player, "You do not have a camera in your inventory.");
                return;
            }

            if (CameraSet == true)
            {
                API.sendChatMessageToPlayer(player, "A camera has already been set.");
            }

            var pos = API.getEntityPosition(player.handle);
            var angle = API.getEntityRotation(player.handle).Z;
            CameraPosition = XyInFrontOfPoint(pos, angle, 1) - new Vector3(0, 0, 0.5);
            var camRot = API.getEntityRotation(player.handle) + new Vector3(0, 0, 180);
            API.sendNotificationToPlayer(player, "A camera has been placed on your position.");
            ChatManager.NearbyMessage(player, 10, "~p~" + character.CharacterName + " sets down a news camera");
            var camera = API.createObject(API.getHashKey("p_tv_cam_02_s"), CameraPosition, CameraRotation);
            character.HasCamera = false;
            CameraSet = true;
        }

        [Command("choppercam")]
        public void choppercam_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            var vehicleHandle = API.getPlayerVehicle(player);
            var veh = VehicleManager.GetVehFromNetHandle(vehicleHandle);
            if (character.Group.Id != veh.GroupId && veh.VehModel != VehicleHash.Maverick)
            {
                API.sendChatMessageToPlayer(player, "You must be in an LSNN chopper to use the chopper camera.");
                return;
            }

            if (ChopperCamToggle == true)
            {
                if (IsBroadcasting == true)
                {
                    foreach (var c in API.getAllPlayers())
                    {
                        Character receivercharacter = API.getEntityData(c, "Character");
                        if (receivercharacter.IsWatchingBroadcast)
                        {

                            API.triggerClientEvent(c, "unwatch_broadcast");
                            receivercharacter.IsWatchingBroadcast = false;
                            API.sendChatMessageToPlayer(c, "~p~The LSNN camera has been turned off.");
                        }
                    }

                    API.sendNotificationToPlayer(player, "The chopper camera has been turned ~r~off~w~.");
                    ChatManager.NearbyMessage(player, 10, "~p~" + character.CharacterName + " has turned off the chopper cam.");
                    CameraSet = false;
                    ChopperCamToggle = false;
                    ChopperRotation.Stop();
                    return;
                }
            }

            if (CameraSet == true)
            {
                API.sendChatMessageToPlayer(player, "A camera has already been set.");
                return;
            }

            CameraSet = true;
            ChopperCamToggle = true;
            Chopper = API.getPlayerVehicle(player);
            CameraPosition = API.getEntityPosition(Chopper) - new Vector3(0, 0, 3);
            CameraRotation = API.getEntityRotation(Chopper);
            API.sendNotificationToPlayer(player, "The chopper camera has been turned ~b~on~w~.");
            ChatManager.NearbyMessage(player, 10, "~p~" + character.CharacterName + " has turned on the chopper cam.");
            ChopperRotation = new Timer { Interval = 3000 };
            ChopperRotation.Elapsed += delegate { UpdateChopperRotation(player); };
            ChopperRotation.Start();
        }


        [Command("pickupcamera")]
        public void pickupcamera_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            if (IsBroadcasting == true)
            {
                API.sendChatMessageToPlayer(player, "A broadcast is in progress.");
            }

            if (CameraSet == false)
            {
                foreach (var p in API.getAllPlayers())
                {
                    Character c = API.getEntityData(p, "Character");
                    if (c.HasCamera == true)
                    {
                        API.sendChatMessageToPlayer(player, "There are no cameras left to pick up.");
                        return;
                    }

                }
                foreach(var v in VehicleManager.Vehicles)
                {
                    if(v.GroupId == character.GroupId)
                    {
                        if (CameraSet == true && player.position.DistanceTo(CameraPosition) > 2f && player.position.DistanceTo(API.getEntityPosition(v.NetHandle)) < 3f)
                        {
                            API.sendChatMessageToPlayer(player, "You can only have one camera.");
                            return;
                        }

                        if (player.position.DistanceTo(API.getEntityPosition(v.NetHandle)) < 3f)
                        {
                            API.sendChatMessageToPlayer(player, "You grabbed a camera from the news vehicle.");
                            ChatManager.NearbyMessage(player, 10, "~p~" + character.CharacterName + " reaches into the news vehicle, pulling out a camera.");
                            character.HasCamera = true;
                            return;
                        }
                    }
                }
                API.sendChatMessageToPlayer(player, "You are too far away from a news vehicle.");
                return;
            }

            var playerPos = API.getEntityPosition(player);
            API.sendNotificationToPlayer(player, "You are carrying a camera.", true);
            ChatManager.NearbyMessage(player, 10, character.CharacterName + "~p~ picks up the news camera.");
            API.deleteObject(player, playerPos, API.getHashKey("p_tv_cam_02_s"));
            character.HasCamera = true;
            CameraSet = false;
            }

        [Command("viewercount")]
        public void viewercount_cmd(Client player)
        {
            var count = 0;
            foreach (var c in PlayerManager.Players)
            {
                if (c.IsWatchingBroadcast == true)
                {
                    count += 1;
                }

            }
            API.sendChatMessageToPlayer(player, count + " people are watching the broadcast.");
        }

        [Command("setlottoprice")]
        public void Setlottoprice(Client player, string amount)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            character.Group.LottoPrice = int.Parse(amount);
            API.sendChatMessageToPlayer(player, "You changed the lotto price to " + int.Parse(amount));
        }

        [Command("buylottoticket")]
        public void Buylottoticket(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            //Check distance from LSNN building main office (need mapping, for now just door)

            if (!IsAtLsnnDoor(player))
            {
                player.sendChatMessage("You must be at the LSNN building to purchase a lotto ticket.");
                return;
            }

            if (Money.GetCharacterMoney(character) < character.Group.LottoPrice)
            {
                API.sendChatMessageToPlayer(player, "You cannot afford a lottery ticket!");
                return;
            }

            foreach (var i in GroupManager.Groups)
            {
                if (i.CommandType == Group.CommandTypeLsnn) { i.LottoSafe += i.LottoPrice; }
            }
            InventoryManager.DeleteInventoryItem(character, typeof(Money), character.Group.LottoPrice);
            character.HasLottoTicket = true;
            API.sendChatMessageToPlayer(player, "You purchased a lottery ticket. Good luck!");
        }
        
        [Command("lotto")]
        public void lotto_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            List<Character> haveLottoTickets = new List<Character>();
            var random = new Random();
            foreach (var c in PlayerManager.Players)
            {
                if (c.HasLottoTicket == true)
                {
                    haveLottoTickets.Add(c);
                    c.HasLottoTicket = false;
                }
            }
            if (haveLottoTickets.Count <= 2)
            {
                API.sendChatMessageToPlayer(player, "There are too few people taking part in the lottery!");
                return;
            }

            int index = random.Next(haveLottoTickets.Count);

            InventoryManager.GiveInventoryItem(haveLottoTickets[index], new Money(), character.Group.LottoSafe);
            API.sendChatMessageToPlayer(player, "~p~ You pick a random name from the list of ticket owners..");
            API.sendChatMessageToAll("~p~The winner of the lotto is ~y~" + haveLottoTickets[index].CharacterName + "~p~. They won " + character.Group.LottoSafe + "!");

        }

        [Command("watchbroadcast")]//HEADLINE FIX
        public void watchbroadcast_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            var camPos = CameraPosition + new Vector3(0, 0, 0.94);
            var camRot = CameraRotation + new Vector3(-1, 0, 180);
            var focusX = CameraPosition.X;
            var focusY = CameraPosition.Y;
            var focusZ = CameraPosition.Z;

            if (character.IsWatchingBroadcast == true)
            {
                API.sendChatMessageToPlayer(player, "You are already watching the broadcast.");
                return;
            }

            if (character.Group.CommandType == Group.CommandTypeLsnn && ChopperCamToggle == true)
            {
                API.triggerClientEvent(player, "watch_chopper_broadcast", CameraPosition, CameraRotation, Headline, Chopper, OffSet, focusX, focusY, focusZ);
                character.IsWatchingBroadcast = true;
                return;
            }

            if (character.Group.CommandType == Group.CommandTypeLsnn && CameraSet == true)
            {
                API.triggerClientEvent(player, "watch_broadcast", camPos, camRot, Headline);
                character.IsWatchingBroadcast = true;
                return;
            }
       
            if (IsBroadcasting == false)
            {
                API.sendChatMessageToPlayer(player, "There is currently no live broadcast.");
                return;
            }

            if (ChopperCamToggle == true)
            {
                
                API.triggerClientEvent(player, "watch_chopper_broadcast", CameraPosition, CameraRotation, Headline, Chopper, OffSet, focusX, focusY, focusZ);
                API.freezePlayer(player, true);
                character.IsWatchingBroadcast = true;
                return;
            }

            API.sendChatMessageToPlayer(player, "You are watching the broadcast. Use /stopwatching to stop watching .");
            API.triggerClientEvent(player, "watch_broadcast", camPos, camRot, Headline, focusX, focusY, focusZ);
            API.freezePlayer(player, true);
            character.IsWatchingBroadcast = true;
        }

        [Command("stopwatching")]
        public void stopwatching_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if(character.IsWatchingBroadcast == false)
            {
                API.sendChatMessageToPlayer(player, "You are not watching any broadcasts.");
            }
            API.triggerClientEvent(player, "unwatch_broadcast");
            API.freezePlayer(player, false);
            character.IsWatchingBroadcast = false;
        }

        [Command("mic")]
        public void mictoggle_cmd(Client player)
        {
            var playerPos = API.getEntityPosition(player);
            Character character = API.getEntityData(player.handle, "Character");

            if (character.HasMic == false && character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, "You do not have a microphone.");
                return;
            }

            if (API.getEntityData(player, "MicStatus") != true)
            {
                API.sendNotificationToPlayer(player, "You are speaking through a microphone.", true);
                API.setEntityData(player, "MicStatus", true);
                var microphone = API.createObject(API.getHashKey("p_ing_microphonel_01"), playerPos, new Vector3());
                API.attachEntityToEntity(microphone, player, "IK_R_Hand", new Vector3(0, 0, 0), new Vector3(0, 0, 0));
                return;
            }
            API.sendNotificationToPlayer(player, "You are no longer speaking through a microphone.");
            API.setEntityData(player, "MicStatus", false);
            API.deleteObject(player, playerPos, API.getHashKey("p_ing_microphonel_01"));

        }

        [Command("givemic")]
        public void micpower_cmd(Client player, string id)
        {
            var target = PlayerManager.ParseClient(id);

            Character sendercharacter = API.getEntityData(player.handle, "Character");
            Character character = API.getEntityData(target.handle, "Character");

            if (sendercharacter.Group == Group.None || sendercharacter.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            if (player.position.DistanceTo(target.position) > 2f)
            {
                API.sendChatMessageToPlayer(player, "You are too far away from that player.");
                return;
            }

            if (character.HasMic == false)
            {
                character.HasMic = true;
                API.sendChatMessageToPlayer(target, "You have been given a microphone. Use /mic to toggle it on/off.");
                API.sendChatMessageToPlayer(player, "You have given a microphone to " + target.name);
                return;
            }
            character.HasMic = false;
            API.sendChatMessageToPlayer(target, "Microphone revoked. You can no longer use the microphone.");
            API.sendChatMessageToPlayer(player, "Microphone removed from " + target.name);

        }

        [Command("createarticle")]
        public void createarticle_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLsnn)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be a member of the LSNN to use that command.");
                return;
            }

            API.sendChatMessageToPlayer(player, "Not yet implemented :(");
            //WILL BE IMPLEMENTING CEF UI FOR WRITING ARTICLES
            //OPTION TO INPUT TITLE AND TEXT
        }

        public void GetPositionInfrontOfEntity(Client player, double x, double y, double distance)
        {
            var playerRot = API.getEntityRotation(player);
            x += (distance * Math.Sin(playerRot.Y));
            y += (distance * Math.Cos(playerRot.Y));
        }

        public static Vector3 XyInFrontOfPoint(Vector3 pos, float angle, float distance)
        {
            Vector3 ret = pos.Copy();
            ret.X += (distance * (float)Math.Sin(angle));
            ret.Y += (distance * (float)Math.Cos(angle));
            return ret;

        }

        public void UpdateChopperRotation(Client player)
        {
            Chopper = API.getPlayerVehicle(player);
            CameraPosition = API.getEntityPosition(Chopper) - new Vector3(0, 0, 3);
            CameraRotation = API.getEntityRotation(Chopper);
            var focusX = CameraPosition.X;
            var focusY = CameraPosition.Y;
            var focusZ = CameraPosition.Z;

            foreach (var p in API.getAllPlayers())
            {
                Character character = API.getEntityData(p, "Character");

                if (character.IsWatchingBroadcast)
                {
                    API.triggerClientEvent(p, "update_chopper_cam", CameraPosition, CameraRotation, Headline, Chopper, OffSet, focusX, focusY, focusZ);
                }
            }
        }

        public bool IsAtLsnnDoor(NetHandle entity)
        {
            return LsnnFrontDoorShape.containsEntity(entity);
        }
    }
}