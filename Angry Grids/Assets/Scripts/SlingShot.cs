using UnityEngine;

using UnityEngine;

public class SlingShotController : MonoBehaviour
{
    public Transform leftPost;
    public Transform rightPost;
    public LineRenderer leftBand;
    public LineRenderer rightBand;
    public LineRenderer trajectoryLine;

    public float forceMultiplier = 0.2f;
    public float maxStretch = 5f;
    public float minLaunch = 0.5f;
    public int trajectoryPoints = 30;
    public float timeStep = 0.1f;

    private Rigidbody rb;
    private bool isDragging = false;
    private Vector3 startPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;

        // setup bands
        leftBand.positionCount = 3;
        rightBand.positionCount = 3;
        leftBand.enabled = false;
        rightBand.enabled = false;

        // setup trajectory
        trajectoryLine.positionCount = trajectoryPoints;
        trajectoryLine.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                isDragging = true;
                rb.isKinematic = true;
                leftBand.enabled = true;
                rightBand.enabled = true;
                trajectoryLine.enabled = true;
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(startPos).z;
            Vector3 worldMouse = Camera.main.ScreenToWorldPoint(mousePos);

            // weighted drag (x stronger than y/z)
            Vector3 drag = worldMouse - startPos;
            drag = new Vector3(drag.x * 1.0f, drag.y * 0.5f, drag.z * 0.5f);

            // clamp stretch
            drag = Vector3.ClampMagnitude(drag, maxStretch);

            transform.position = startPos + drag;

            UpdateBands();
            UpdateTrajectory();
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            rb.isKinematic = false;

            Vector3 pullVector = startPos - transform.position;

            if (pullVector.magnitude < minLaunch)
            {
                transform.position = startPos;
                rb.linearVelocity = Vector3.zero;
            }
            else
            {
                rb.AddForce(pullVector * pullVector.magnitude * forceMultiplier, ForceMode.Impulse);
            }

            leftBand.enabled = false;
            rightBand.enabled = false;
            trajectoryLine.enabled = false;
        }
    }

    void UpdateBands()
    {
        Vector3 midLeft = (leftPost.position + transform.position) / 2;
        midLeft.y -= 0.5f;

        Vector3 midRight = (rightPost.position + transform.position) / 2;
        midRight.y -= 0.5f;

        leftBand.SetPosition(0, leftPost.position);
        leftBand.SetPosition(1, midLeft);
        leftBand.SetPosition(2, transform.position);

        rightBand.SetPosition(0, rightPost.position);
        rightBand.SetPosition(1, midRight);
        rightBand.SetPosition(2, transform.position);
    }

    void UpdateTrajectory()
    {
        Vector3 pullVector = startPos - transform.position;
        Vector3 launchVelocity = pullVector * pullVector.magnitude * forceMultiplier / rb.mass;

        Vector3 pos = transform.position;
        Vector3 vel = launchVelocity;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            trajectoryLine.SetPosition(i, pos);
            vel += Physics.gravity * timeStep;
            pos += vel * timeStep;
        }

        // make it dotted by tiling the texture
        trajectoryLine.material.mainTextureScale = new Vector2(trajectoryPoints / 2f, 1);
    }
}