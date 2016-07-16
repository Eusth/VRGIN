using UnityEngine;
using System.Collections;
using Leap;
using System.Runtime.InteropServices;

namespace Leap.Unity{

  [Guid("8bcd03e0-0992-e084-c8be-61565d44b8bd")]
  public class HandEnableDisable : HandTransitionBehavior {
    protected override void Awake() {
      base.Awake();
      gameObject.SetActive(false);
    }

  	protected override void HandReset() {
      gameObject.SetActive(true);
  	}
  
  	protected override void HandFinish () {
  		gameObject.SetActive(false);
  	}
  	
  }
}
