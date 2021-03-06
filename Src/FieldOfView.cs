using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
  public float viewRadius;
  [Range (0, 360)]
  public float viewAngle;

  public LayerMask targetMask;
  public LayerMask obstacleMask;

  [HideInInspector]
  public List<Transform> visibleTargets = new List<Transform> ();

  public float meshResolution;
  public int edgeResolveIterations;
  public float edgeDstThreshold;

  public float maskCutawayDst = .1f;

  public MeshFilter viewMeshFilter;
  Mesh viewMesh;

  void Start ()
  {
    viewMesh = new Mesh ();
    viewMesh.name = "View Mesh";
    viewMeshFilter.mesh = viewMesh;

    StartCoroutine ("FindTargetsWithDelay", .2f);
  }

  IEnumerator FindTargetsWithDelay (float delay)
  {
    while (true)
    {
      yield return new WaitForSeconds (delay);
      FindVisibleTargets ();
    }
  }

  void LateUpdate ()
  {
    DrawFieldOfView ();
  }

  void FindVisibleTargets ()
  {
    visibleTargets.Clear ();
    var targetsInViewRadius = Physics.OverlapSphere (transform.position, viewRadius, targetMask);

    for (var i = 0; i < targetsInViewRadius.Length; i++)
    {
      var target = targetsInViewRadius[i].transform;
      var dirToTarget = (target.position - transform.position).normalized;
      if (Vector3.Angle (transform.forward, dirToTarget) < viewAngle / 2)
      {
        var dstToTarget = Vector3.Distance (transform.position, target.position);
        if (!Physics.Raycast (transform.position, dirToTarget, dstToTarget, obstacleMask))
        {
          visibleTargets.Add (target);
        }
      }
    }
  }

  void DrawFieldOfView ()
  {
    var stepCount = Mathf.RoundToInt (viewAngle * meshResolution);
    var stepAngleSize = viewAngle / stepCount;
    var viewPoints = new List<Vector3> ();
    var oldViewCast = new ViewCastInfo ();
    for (var i = 0; i <= stepCount; i++)
    {
      var angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
      var newViewCast = ViewCastInfo.GetViewCast (transform, angle, viewRadius, obstacleMask);

      if (i > 0)
      {
        var edgeDstThresholdExceeded = Mathf.Abs (oldViewCast.distance - newViewCast.distance) > edgeDstThreshold;
        if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
        {
          var edge = EdgeInfo.FindEdge (transform, oldViewCast, newViewCast, viewRadius, edgeResolveIterations, edgeDstThreshold, obstacleMask);
          if (edge.pointA != Vector3.zero)
          {
            viewPoints.Add (edge.pointA);
          }
          if (edge.pointB != Vector3.zero)
          {
            viewPoints.Add (edge.pointB);
          }
        }
      }
      viewPoints.Add (newViewCast.point);
      oldViewCast = newViewCast;
    }

    var vertexCount = viewPoints.Count + 1;
    var vertices = new Vector3[vertexCount];
    var triangles = new int[(vertexCount - 2) * 3];

    vertices[0] = Vector3.zero;
    for (var i = 0; i < vertexCount - 1; i++)
    {
      vertices[i + 1] = transform.InverseTransformPoint (viewPoints[i]) + Vector3.forward * maskCutawayDst;

      if (i < vertexCount - 2)
      {
        triangles[i * 3] = 0;
        triangles[i * 3 + 1] = i + 1;
        triangles[i * 3 + 2] = i + 2;
      }
    }

    viewMesh.Clear ();

    viewMesh.vertices = vertices;
    viewMesh.triangles = triangles;
    viewMesh.RecalculateNormals ();
  }
}
