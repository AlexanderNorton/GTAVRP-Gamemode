﻿API.onServerEventTrigger.connect((eventName, args) => {

});

var myBrowser = null;

API.onPlayerEnterVehicle.connect((vehicle) => {
	var res = API.getScreenResolution();
	var width = 600;
	var height = 300;
	myBrowser = API.createCefBrowser(width, height);
	API.waitUntilCefBrowserInit(myBrowser);
	API.setCefBrowserPosition(myBrowser,
		300,
		res.Height - height + 40);
	API.loadPageCefBrowser(myBrowser, "speed_fuel_system/SpeedoFuel.html");
	API.setCefDrawState(true);
	API.waitUntilCefBrowserLoaded(myBrowser);
});

function loaded() {
	var vehicle = API.getPlayerVehicle(API.getLocalPlayer());
	var speed = API.getVehicleMaxSpeed(API.getEntityModel(vehicle));
	var intSpeed = Math.round(speed * 4); //The max speed from the game is so small for some reason, doing this to get it big xD
	API.sendChatMessage("Speed: " + intSpeed);
	myBrowser.call("setupSpeed", intSpeed);
}

API.onPlayerExitVehicle.connect((vehicle) => {
	API.destroyCefBrowser(myBrowser);
	API.setCefDrawState(false);
	myBrowser = null;
});

var lastPos = "";
var posUpdateTick = 0;

API.onUpdate.connect(() => {
	if (myBrowser !== null) {
		var vehicule = API.getPlayerVehicle(API.getLocalPlayer());
		var velocity = API.getEntityVelocity(vehicule);
		var speed = Math.sqrt(
			velocity.X * velocity.X +
			velocity.Y * velocity.Y +
			velocity.Z * velocity.Z
		);
		speed = Math.floor(speed * 3.6);
		myBrowser.call("setSpeed", speed);
	}

	if (lastPos !== "") {
		API.drawText("~w~" + lastPos, 20, API.getScreenResolution().Height - 300, 1, 115, 186, 131, 255, 4, 0, false, true, 0);
	}

	posUpdateTick += 1;
	if (posUpdateTick === 120) {
		posUpdateTick = 0;
		lastPos = API.getZoneName(API.getEntityPosition(API.getLocalPlayer()));
	}
});