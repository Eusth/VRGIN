﻿using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;

namespace Leap.Unity {

  /**
   * Base class for detectors.
   * 
   * A Detector is an object that observes some aspect of a scene and reports true
   * when the specified conditions are met. Typically these conditions involve hand
   * information, but this is not required.
   * 
   * Detector implementations must call Activate() when their conditions are met and
   * Deactivate() when those conditions are no longer met. Implementations should
   * also call Deactivate() when they, or the object they are a component of become disabled.
   * Implementations can call Activate() and Deactivate() more often than is strictly necessary.
   * This Detector base class keeps track of the IsActive status and only dispatches events
   * when the status changes.
   * 
   * @since 4.1.2
   */
  public class Detector : MonoBehaviour {
    /** The current detector state. 
     * @since 4.1.2 
     */
    public bool IsActive{ get{ return _isActive;}}
    private bool _isActive = false;
    /** Whether to draw the detector's Gizmos for debugging. (Not every detector provides gizmos.)
     * @since 4.1.2 
     */
    [Tooltip("Draw this detector's Gizmos, if any. (Gizmos must be on in Unity edtor, too.)")]
    public bool ShowGizmos = true;
        /** Dispatched when the detector activates (becomes true). 
         * @since 4.1.2
         */
        [Tooltip("Dispatched when condition is detected.")]
        public UnityEvent OnActivate = new UnityEvent();
    /** Dispatched when the detector deactivates (becomes false). 
     * @since 4.1.2
     */
    [Tooltip("Dispatched when condition is no longer detected.")]
    public UnityEvent OnDeactivate = new UnityEvent();

        /**
        * Invoked when this detector activates.
        * Subclasses must call this function when the detector's conditions become true.
        * @since 4.1.2
        */
        public virtual void Activate(){
      if (!IsActive) {
        _isActive = true;
        OnActivate.Invoke();
      }
    }

    /**
    * Invoked when this detector deactivates.
    * Subclasses must call this function when the detector's conditions change from true to false.
    * @since 4.1.2
    */
    public virtual void Deactivate(){
      if (IsActive) {
        _isActive = false;
        OnDeactivate.Invoke();
      }
    }
  }
}
