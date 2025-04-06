extends Sprite2D

@export var camera_name: String = ""
var camera: CameraFeed

# Called when the node enters the scene tree for the first time.
func _ready():
	print("cameras:")
	for feed in CameraServer.feeds():
		feed.set_format(0,{"output":"grayscale"})
		var name = feed.get_name()
		print(name)
		
		# if camera_name is left empty, use the first available camera
		if camera == null and (camera_name == "" or name == camera_name):
			camera = feed
	
	if camera == null:
		print("no matching camera")
		return
		
	print("using camera ", camera, " (", camera.get_name(), ")")
	
	camera.feed_is_active = true
	
	texture = CameraTexture.new()
	
	texture.camera_feed_id = camera.get_id()

func _process(delta: float) -> void:
	pass
	#scale = texture.get_size()
