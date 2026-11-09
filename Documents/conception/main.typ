#import "@local/yaqi-report:0.1.0": *

#show: content => report(
  content,
  title: "Document de conception",
  authors: ("Yaqi ZHANG","Matis BEGOT", "Rémi BONNAFE"),
  institution: "Université de Montpellier",
  program: "Master 2 IMAGINE",
  course: "HAI928I-Projet-VR/AR",
  teacher: none,
  date: datetime.today(),
  language: "fr",
  font-size: 14pt,
  outline-depth: 2,
  header-left: none,
  header-right: none,
  header-line: false,
)

= Identité du projet

Cette application de numérisation et de visite virtuelle permet de scanner un espace réel avec un téléphone. Elle utilise le 3D Gaussian Splatting pour le reconstruire sous la forme d’une scène 3D que l’on peut explorer librement sur téléphone, sur ordinateur ou avec un casque de réalité virtuelle.

= Piliers de conception

Curiosité · Émerveillement · Immersion

= Résultats

L’utilisateur se déplace d’abord dans un espace réel pour le capturer sous plusieurs angles avec son téléphone. Il peut ensuite redécouvrir ce même espace à deux échelles. En réalité augmentée, il tourne autour d’une maquette posée sur une table. Avec un casque de réalité virtuelle, il entre dans la scène reconstruite, se téléporte d’un point à un autre et regarde autour de lui. Le projet permet de conserver une trace visuelle d’un lieu réel, de le faire visiter à distance et d’en garder le souvenir. Par exemple, un enseignant peut utiliser la reconstruction d’un laboratoire pour présenter à ses étudiants les équipements et l’aménagement de la salle. Les étudiants peuvent choisir où se placer et sous quel angle les observer.

= Fonctionnalités

- Numérisation sur téléphone. AR Foundation et ARKit assurent l’aperçu de la caméra et le suivi des mouvements. L’application sélectionne les images clés en fonction des déplacements du téléphone et les enregistre au format JPEG.
- Envoi et reconstruction. L’application envoie les photos et affiche l’état de la tâche. La station de travail utilise COLMAP pour estimer les poses des caméras et produire un nuage de points clairsemé. Elle utilise ensuite Splatfacto dans Nerfstudio pour entraîner la scène gaussienne et l’exporter au format Gaussian PLY.
- Bibliothèque de scènes. L’utilisateur peut suivre l’avancement de la reconstruction, télécharger le résultat et rouvrir les scènes déjà enregistrées sur le téléphone.
- Exploration sur plusieurs appareils. Le téléphone propose une vue 3D locale et un mode de réalité augmentée. Le client Unity pour ordinateur permet de parcourir la scène au clavier et à la souris, ou en réalité virtuelle avec un casque connecté.


= Interface

- *Numérisation sur téléphone.* Appuyer sur « Démarrer le scan », puis se déplacer lentement en suivant les indications à l’écran. Appuyer sur « Terminer le scan » pour envoyer les photos.
- *Vue 3D locale sur téléphone.* Ouvrir le modèle depuis la bibliothèque de scènes. Faire glisser un doigt pour changer d’angle de vue et pincer ou écarter deux doigts pour zoomer.
- *Réalité augmentée sur téléphone.* Pointer le téléphone vers une table, toucher l’écran pour placer la scène miniature, puis se déplacer autour de la table pour l’observer.
- *Exploration dans Unity sur ordinateur.* Utiliser le clavier pour se déplacer et la souris pour orienter la vue.
- *Visite en réalité virtuelle.* Mettre le casque, tourner la tête pour observer la scène et utiliser une manette pour se téléporter dans les zones prévues. Le casque est connecté au client Unity via OpenXR.

= Style artistique

La scène est reconstruite à partir d’un lieu réel et conserve autant que possible les couleurs, l’éclairage et les détails des photos d’origine. Sur le téléphone, la vue 3D locale utilise un fond neutre et le mode de réalité augmentée affiche la scène miniature sur une table réelle. L’interface présente surtout les consignes de capture, l’état de la tâche et les commandes nécessaires, afin que l’utilisateur puisse se concentrer sur la scène.

= Musique
Le son sert principalement à confirmer les actions de l’utilisateur. Un signal sonore bref peut accompagner le début et la fin du scan, tandis que l’état correspondant s’affiche à l’écran. Pendant la capture, ce signal permet à l’utilisateur de savoir si son action a bien été prise en compte tout en gardant son attention sur la scène.

= Feuille de route du développement:

#report-table(
  headers:([Jalon],[Date],[Étape]),
  columns: (1fr, 1fr, 2fr),
  rows:(
    ([Jalon 1],[19/11/2026],[Développer dans Unity une application mobile qui fonctionne sur un téléphone, permet de scanner une scène et exporte des photos prises sous plusieurs angles pour la reconstruction. Développer ensuite la visualisation en réalité augmentée d’une scène reconstruite au format PLY.]),
    ([Jalon 2],[30/11/2026],[Estimer sur la station de travail les poses des caméras à partir des photos avec COLMAP, puis préparer les données nécessaires à l’entraînement de la scène gaussienne 3D.]),
    ([Jalon 3],[10/12/2026],[Valider le processus de reconstruction en entraînant une scène gaussienne avec Splatfacto, puis en exportant le résultat au format Gaussian PLY.]),
    ([Jalon 4],[20/12/2026],[Finaliser la visite en réalité virtuelle dans le client Unity pour ordinateur et intégrer à l’application mobile le chargement des scènes reconstruites sur la station de travail.]),
    ([Jalon 5],[30/12/2026],[Tester l’ensemble du processus, affiner les fonctionnalités, corriger les bugs et livrer la version finale.])
  ),
  cell-stroke: 0.4pt + gray,
  cell-align: center + horizon,
  caption: [Feuille de route du développement]
)

