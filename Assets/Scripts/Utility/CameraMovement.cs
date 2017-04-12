using UnityEngine;

public class CameraMovement : MonoBehaviour {
	private void Start() {
		currentx = transform.rotation.eulerAngles.x;
		currenty = transform.rotation.eulerAngles.y;
		targetx = transform.rotation.eulerAngles.x;
		targety = transform.rotation.eulerAngles.y;
	}

    public new bool enabled = false;
    public float speed = 150f;
	private float currentx = 0;
	private float currenty = 0;
	private float targetx = 0;
	private float targety = 0;
	private float speedx = 10;
	private float speedy = 10;
    
	private void Update() {
        if (enabled) {
            HandleCameraMotion();
            HandleMouse();
        } else {
            currentx = transform.rotation.eulerAngles.x;
            currenty = transform.rotation.eulerAngles.y;
            targetx = transform.rotation.eulerAngles.x;
            targety = transform.rotation.eulerAngles.y;
        }
	}

	private void HandleCameraMotion() {
		float x_component = 0;
		float y_component = 0;
		float active_speed = speed;

		if(Input.GetKey(KeyCode.LeftShift)) {
			active_speed *= 2;
		}

		float translation = active_speed * Time.deltaTime;

		if(Input.GetKey(KeyCode.A)) {
			x_component -= translation;
		}
		if(Input.GetKey(KeyCode.D)) {
			x_component += translation;
		}
		if(Input.GetKey(KeyCode.W)) {
			y_component += translation;
		}
		if(Input.GetKey(KeyCode.S)) {
			y_component -= translation;
		}

		Vector3 vector = new Vector3(x_component, 0, y_component);
		vector = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0) * vector;
		transform.position += vector;

		if(Input.GetKey(KeyCode.Q))
			transform.position += Vector3.up * Time.deltaTime * active_speed;
		if(Input.GetKey(KeyCode.Z))
			transform.position -= Vector3.up * Time.deltaTime * active_speed;
	}

	private void HandleMouse() {
		if(Input.GetMouseButton(1)) {
			targety += Input.GetAxis("Mouse X") * 2;
			targetx += Input.GetAxis("Mouse Y") * -2;
			//Cursor.lockState = CursorLockMode.Locked;
		} else {
			//Cursor.lockState = CursorLockMode.None;
		}

		currentx = Mathf.Clamp(currentx, -90, 90);

		currentx = Mathf.SmoothDamp(currentx, targetx, ref speedx, 0);
		currenty = Mathf.SmoothDamp(currenty, targety, ref speedy, 0);

		transform.rotation = Quaternion.Euler(new Vector3(currentx, currenty, 0));
	}
}