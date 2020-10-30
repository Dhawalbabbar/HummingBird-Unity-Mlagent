using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///manages single flower with nector
public class Flower : MonoBehaviour
{
    [Tooltip("colour when the flower is full")]
    public Color fullFlowerColor=new Color(1f,0f,.3f);

    [Tooltip("colour when the flower is empty")]
    public Color emptyFlowerColor=new Color(0.5f,0f,1f);

    ///<summary>
    ///The Trigger collider of nector
    ///</summary>
    // [HideInInspector]
    public Collider nectorCollider;
    // solid collider of petels
    private Collider flowerCollider;

    //flower material
    private Material flowerMaterial;

    public Vector3 FlowerUpVector{
        get{
            return nectorCollider.transform.up;
        }
    }
    public Vector3 FlowerCenterPosition{
        get{
            return nectorCollider.transform.position;
        }
    }
    public float NectorAmount{get;private set;}
    public bool HasNector{
        get{
            return NectorAmount>0f;
        }
    }

    public float Feed(float amount){
        // track how much nector was taken
        float nectorTaken=Mathf.Clamp(amount,0f,NectorAmount);
        //sub nector
        NectorAmount-=amount;
        if(NectorAmount<=0){
            NectorAmount=0;
            //disable the flower and nector colliders
            flowerCollider.gameObject.SetActive(false);
            nectorCollider.gameObject.SetActive(false);

            //change flower colour
            flowerMaterial.SetColor("_BaseColor",emptyFlowerColor);

        }
        //return amount of nector taken
        return nectorTaken;
    }

    public void ResetFlower(){
        //refill the nector
        NectorAmount=1f;
        flowerCollider.gameObject.SetActive(true);
        nectorCollider.gameObject.SetActive(true);

        flowerMaterial.SetColor("_BaseColor",fullFlowerColor);
    }

    private void Awake() {
        //find flower mesh rendrer and get material
        MeshRenderer meshRenderer=GetComponent<MeshRenderer>();
        flowerMaterial=meshRenderer.material;

        //find flower and nector collider
        flowerCollider=transform.Find("FlowerCollider").GetComponent<Collider>();
        nectorCollider=transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }

}
