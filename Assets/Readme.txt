Idea:
	For the runtime level editor, we needed a mechanism for creating asset previews in runtime, so that the player could view them in the inventory and select the one they need to place in the level.

Implementation:

	I created ItemPreview plugin. It spawns objects one by one at a given point in the AutoRender scene and renders the image from the camera to the RT_ItemPreviewCamOutput texture. Then the resulting image is wrapped in a sprite and sent to the runtime cache and converted to png and placed on disk. The main idea is a factory template, since we literally need a conveyor to create something, in this case, images.

	The code was a bit more complex, but I had to remove support for recoloring objects since it requires a mapped model, shader, and color schemes, all of which are covered by the NDA.


There to start:

	Just open StartClient scene and click Run. Result can bee seen at Path.Combine(Application.persistentDataPath, "ItemPreview").


Project structure:

Scenes:
	StartClient     - "game" launcher. It contains ClientStarter monoBehaviour on the object with the same name.
	GameObjectCache - empty scene for prefabs loading from disk.
	AutoRender      - asset preview generation configuration.

Resources:
	Contains prefabs and assetGuidMap object which is needed for assetLoadingManager (guids help to reduce network consumption and also unifies handling of remote and local assets.
