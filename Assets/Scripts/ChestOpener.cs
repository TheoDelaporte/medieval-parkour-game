using System.Collections;
using UnityEngine;

public class ChestOpener : MonoBehaviour
{
    public Animation chestAnimation;

    void Start()
    {
        // Assurez-vous que l'Animation est attachée
        if (chestAnimation == null)
        {
            chestAnimation = GetComponent<Animation>();
        }

        // Lancez la coroutine pour ouvrir et fermer le coffre en boucle
        StartCoroutine(OpenCloseChest());
    }

    IEnumerator OpenCloseChest()
    {
        while (true)
        {
            // Jouez l'animation d'ouverture
            chestAnimation.Play("ChestOpenAnimation");

            // Attendez la fin de l'animation d'ouverture
            yield return new WaitForSeconds(chestAnimation["ChestOpenAnimation"].length);

            // Jouez l'animation de fermeture
            chestAnimation.Play("ChestCloseAnimation");

            // Attendez la fin de l'animation de fermeture
            yield return new WaitForSeconds(chestAnimation["ChestCloseAnimation"].length);
        }
    }
}


