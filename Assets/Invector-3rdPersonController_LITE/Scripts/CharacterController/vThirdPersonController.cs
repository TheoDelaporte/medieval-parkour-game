using UnityEngine;
using TMPro;
using System.Collections;
using System.Diagnostics;

namespace Invector.vCharacterController
{
    public class vThirdPersonController : vThirdPersonAnimator
    {
        public Animator playerAnim; // Référence à l'Animator du joueur
        private int numberOfCoins; // Nombre de pièces collectées
        public TextMeshProUGUI countText; // Référence au texte affichant le nombre de pièces
        public TextMeshProUGUI livesText; // Référence au texte affichant le nombre de pièces
        public GameObject winTextObject; // Référence au texte affichant la fin de la partie et la victoire
        public GameObject loseTextObject; // Référence au texte affichant la fin de la partie et la défaite
        public GameObject livesTextObject;
        public GameObject countTextObject;
        private bool hit = false; // Variable pour indiquer si le personnage a déjà été touché par une flèche
        private int lives = 3; // Nombre de vies initial du personnage
        private bool isInvulnerable = false; // Indique si le personnage est invulnérable après avoir été touché par une flèche
        public AudioClip LandingAudioClip;
        private CharacterController _controller;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [System.Obsolete]
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.LoadLevel("Main Menu");
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                Application.LoadLevel("playground");
            }
        }

        void Start()
        {
            winTextObject.SetActive(false); // Fait disparaître le texte de victoire
            loseTextObject.SetActive(false); // Fait disparaître le texte de défaite
            // Initialise le nombre de pièces à 0 lorsque la partie commence
            numberOfCoins = 0;
            SetCountText(); // Met à jour le texte affichant le nombre de pièces collectées      
            SetLivesText(); // Met à jour l'affichage du nombre de vies
        }

        // Contrôle le mouvement racine de l'Animator
        public virtual void ControlAnimatorRootMotion()
        {
            if (!this.enabled) return;

            if (inputSmooth == Vector3.zero)
            {
                transform.position = animator.rootPosition;
                transform.rotation = animator.rootRotation;
            }

            if (useRootMotion)
                MoveCharacter(moveDirection);
        }

        // Contrôle le type de locomotion
        public virtual void ControlLocomotionType()
        {
            if (lockMovement) return;

            if (locomotionType.Equals(LocomotionType.FreeWithStrafe) && !isStrafing || locomotionType.Equals(LocomotionType.OnlyFree))
            {
                SetControllerMoveSpeed(freeSpeed);
                SetAnimatorMoveSpeed(freeSpeed);
            }
            else if (locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.FreeWithStrafe) && isStrafing)
            {
                isStrafing = true;
                SetControllerMoveSpeed(strafeSpeed);
                SetAnimatorMoveSpeed(strafeSpeed);
            }

            if (!useRootMotion)
                MoveCharacter(moveDirection);
        }

        // Contrôle le type de rotation
        public virtual void ControlRotationType()
        {
            if (lockRotation) return;

            bool validInput = input != Vector3.zero || (isStrafing ? strafeSpeed.rotateWithCamera : freeSpeed.rotateWithCamera);

            if (validInput)
            {
                // Calcule l'input smooth
                inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);

                Vector3 dir = (isStrafing && (!isSprinting || sprintOnlyFree == false) || (freeSpeed.rotateWithCamera && input == Vector3.zero)) && rotateTarget ? rotateTarget.forward : moveDirection;
                RotateToDirection(dir);
            }
        }

        // Met à jour la direction de déplacement
        public virtual void UpdateMoveDirection(Transform referenceTransform = null)
        {
            if (input.magnitude <= 0.01)
            {
                moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
                return;
            }

            if (referenceTransform && !rotateByWorld)
            {
                var right = referenceTransform.right;
                right.y = 0;
                var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
                moveDirection = (inputSmooth.x * right) + (inputSmooth.z * forward);
            }
            else
            {
                moveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        // Gère la course
        public virtual void Sprint(bool value)
        {
            var sprintConditions = (input.sqrMagnitude > 0.1f && isGrounded &&
                !(isStrafing && !strafeSpeed.walkByDefault && (horizontalSpeed >= 0.5 || horizontalSpeed <= -0.5 || verticalSpeed <= 0.1f)));

            if (value && sprintConditions)
            {
                if (input.sqrMagnitude > 0.1f)
                {
                    if (isGrounded && useContinuousSprint)
                    {
                        isSprinting = !isSprinting;
                    }
                    else if (!isSprinting)
                    {
                        isSprinting = true;
                    }
                }
                else if (!useContinuousSprint && isSprinting)
                {
                    isSprinting = false;
                }
            }
            else if (isSprinting)
            {
                isSprinting = false;
            }
        }

        // Permet de passer en mode de déplacement latéral
        public virtual void Strafe()
        {
            isStrafing = !isStrafing;
        }

        // Gère le saut
        public virtual void Jump()
        {
            // Déclenche le saut
            jumpCounter = jumpTimer;
            isJumping = true;

            // Déclenche les animations de saut
            if (input.sqrMagnitude < 0.1f)
                animator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                animator.CrossFadeInFixedTime("JumpMove", .2f);
        }

        // Gère les collisions avec d'autres objets
        void OnTriggerEnter(Collider other)
        {
            System.Diagnostics.Debug.WriteLine("This is a log");

            if (other.gameObject.CompareTag("Coin")) // Si le joueur entre en collision avec une pièce
            {
                other.gameObject.SetActive(false); // Désactive la pièce
                numberOfCoins = numberOfCoins + 1; // Augmente le nombre de pièces collectées
                SetCountText(); // Met à jour le texte affichant le nombre de pièces
            }
            else if (other.gameObject.CompareTag("Chest")) // Si le joueur entre en collision avec un coffre
            {
                other.gameObject.SetActive(false); // Désactive le coffre
                numberOfCoins = numberOfCoins + 3; // Augmente le nombre de pièces collectées
                SetCountText(); // Met à jour le texte affichant le nombre de pièces
            }
            else if (
                other.gameObject.CompareTag("spike") ||
                other.gameObject.CompareTag("Axe") ||
                other.gameObject.CompareTag("Fire") ||
                other.gameObject.CompareTag("Poison") &&
                !hit && !isInvulnerable
                ) // Si le joueur entre en collision avec une flèche et n'a pas déjà été touché
            {
                //other.gameObject.SetActive(false); // Désactive la flèche
                //hitByArrow = true; // Marque que le personnage a été touché par une flèche
                lives--; // Réduit le nombre de vies
                //lives = 2;

                // Met à jour l'affichage du nombre de vies
                SetLivesText();

                if (lives <= 0) // Si le personnage n'a plus de vie, mettre en œuvre la logique de défaite
                {
                    // Logique de défaite
                    loseTextObject.SetActive(true);
                    livesTextObject.SetActive(false);
                    countTextObject.SetActive(false);
                }
                else
                {
                    StartCoroutine(InvulnerabilityDelay()); // Démarre la coroutine pour rendre le personnage invulnérable pendant un certain temps
                }
            }
        }

        IEnumerator InvulnerabilityDelay()
        {
            isInvulnerable = true; // Rend le personnage invulnérable

            // Attendre un certain délai
            yield return new WaitForSeconds(4f); // 4 secondes se déroulent entre chaque perte de vie

            isInvulnerable = false; // Rend le personnage vulnérable à nouveau
            hit = false; // Réinitialise la variable de collision de la flèche
        }

        // Met à jour le texte affichant le nombre de pièces collectées
        void SetCountText()
        {
            countText.text = numberOfCoins.ToString();

            if (numberOfCoins >= 17)
            {
                winTextObject.SetActive(true);
                livesTextObject.SetActive(false);
                countTextObject.SetActive(false);
                CharacterController characterController = GetComponent<CharacterController>();
            }
        }

        // Méthode pour désactiver les touches Z, Q, S, D
        void DisablePlayerControls()
        {
            // Obtenez le composant CharacterController ou tout autre composant de mouvement du joueur
            CharacterController characterController = GetComponent<CharacterController>();

            // Désactivez la possibilité de mouvement du joueur
            if (characterController != null)
            {
                characterController.enabled = false;
            }
        }

        // Méthode pour mettre à jour l'affichage du nombre de vies
        void SetLivesText()
        {
            livesText.text = "";
            // Boucle pour générer des caractères de cœur
            for (int i = 0; i < lives; i++)
            {
                livesText.text += "<3 "; // Ajoute un caractère de cœur pour chaque vie
            }
        }
    }
}
