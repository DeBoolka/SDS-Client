using UnityEngine;

public class LivingEntity : MonoBehaviour {
    
    float amountRemaining = 1;
    const float consumeSpeed = 8;

    public int colourMaterialIndex;
    public Species species;
    public Material material;

    public Coord coord;
    //
    [HideInInspector]
    public int mapIndex;
    [HideInInspector]
    public Coord mapCoord;

    protected bool dead;

    public virtual void Init (Coord coord) {
        this.coord = coord;
        transform.position = Environment.tileCentres[coord.x, coord.y];

        // Set material to the instance material
        var meshRenderer = transform.GetComponentInChildren<MeshRenderer> ();
        for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
        {
            if (meshRenderer.sharedMaterials[i] == material) {
                material = meshRenderer.materials[i];
                break;
            }
        }
    }
    
    public float Consume (float amount) {
        float amountConsumed = Mathf.Max (0, Mathf.Min (amountRemaining, amount));
        amountRemaining -= amount * consumeSpeed;

        transform.localScale = Vector3.one * amountRemaining;

        if (amountRemaining <= 0) {
            Die (CauseOfDeath.Eaten);
        }

        return amountConsumed;
    }

    protected virtual void Die (CauseOfDeath cause) {
        if (!dead) {
            dead = true;
            Environment.RegisterDeath (this);
            Destroy (gameObject);
        }
    }
}