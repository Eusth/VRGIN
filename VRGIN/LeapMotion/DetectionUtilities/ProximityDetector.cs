﻿using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;

namespace Leap.Unity{

  /**
   * Detects when the parent GameObject is within the specified distance
   * of one of the target objects.
   * @since 4.1.2
   */
  public class ProximityDetector : Detector {
    /**
     * The interval at which to check palm direction.
     * @since 4.1.2
     */
    [Tooltip("The interval in seconds at which to check this detector's conditions.")]
    public float Period = .1f; //seconds
                               /**
                                * Dispatched when the proximity check succeeds.
                                * The ProximityEvent object provides a reference to the proximate GameObject. 
                                * @since 4.1.2
                                */
        [Tooltip("Dispatched when close enough to a target.")]
        public ProximityEvent OnProximity = new ProximityEvent();

        /**
         * The list of objects which can activate the detector by proximity.
         * @since 4.1.2
         */
        [Tooltip("The list of target objects.")]
        public GameObject[] TargetObjects;

    /**
     * The distance in meters between this game object and the target game object that
     * will pass the proximity check.
     * @since 4.1.2
     */
    [Tooltip("The target distance in meters to activate the detector.")]
    public float OnDistance = .01f; //meters

    /**
     * The distance in meters between this game object and the target game object that
     * will turn off the detector. 
     * @since 4.1.2
     */
    [Tooltip("The distance in meters at which to deactivate the detector.")]
    public float OffDistance = .015f; //meters

    /**
     * The object that is close to the activated detector.
     * 
     * If more than one target object is within the required distance, it is
     * undefined which object will be current. Set to null when no targets
     * are close enough.
     * @since 4.1.2
     */
    public GameObject CurrentObject { get { return _currentObj; } }

    private IEnumerator proximityWatcherCoroutine;
    private GameObject _currentObj = null;

    void Awake(){
      proximityWatcherCoroutine = proximityWatcher();
    }

    void OnEnable () {
        StopCoroutine(proximityWatcherCoroutine);
        StartCoroutine(proximityWatcherCoroutine);
    }
  
    void OnDisable () {
      StopCoroutine(proximityWatcherCoroutine);
    }

    IEnumerator proximityWatcher(){
      bool proximityState = false;
      float onSquared, offSquared; //Use squared distances to avoid taking square roots
      while(true){
        onSquared = OnDistance * OnDistance;
        offSquared = OffDistance * OffDistance;
        if(_currentObj != null){
          if(distanceSquared(_currentObj) > offSquared){
            _currentObj = null;
            proximityState = false;
          }
        } else {
          for(int obj = 0; obj < TargetObjects.Length; obj++){
            GameObject target = TargetObjects[obj];
            if(distanceSquared(target) < onSquared){
              _currentObj = target;
              proximityState = true;
              OnProximity.Invoke(_currentObj);
              break; // pick first match
            }
          }
        }
        if(proximityState){
          Activate();
        } else {
          Deactivate();
        }
        yield return new WaitForSeconds(Period);
      }
    }

    private float distanceSquared(GameObject target){
      Collider targetCollider = target.GetComponent<Collider>();
      Vector3 closestPoint;
      if(targetCollider != null){
        closestPoint = targetCollider.ClosestPointOnBounds(transform.position);
      } else {
        closestPoint = target.transform.position;
      }
      return (closestPoint - transform.position).sqrMagnitude;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos(){
      if(IsActive){
        Gizmos.color = Color.green;
      } else {
        Gizmos.color = Color.red;
      }
      Gizmos.DrawWireSphere(transform.position, OnDistance);
      Gizmos.color = Color.blue;
      Gizmos.DrawWireSphere(transform.position, OffDistance);
    }
    #endif
  }

  /**
   * An event class that is dispatched by a ProximityDetector when the detector's
   * game object comes close enough to a game object in its target list.
   * The event parameters provide the proximate game object.
   * @since 4.1.2
   */
  [System.Serializable]
  public class ProximityEvent : UnityEvent <GameObject> {}
}