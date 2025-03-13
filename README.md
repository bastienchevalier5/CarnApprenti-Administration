# Présentation du Projet

Ce projet est une application de bureau développée en C# avec Blazor MAUI et est la partie Administration de l'application web CarnApprenti.

## Fonctionnalités

- Connexion pour les administrateurs et le secteur qualité
- Profil et modification des informations (nom, prénom, email, mot de passe)
- Gestion des utilisateurs (ajout, modification, suppression et importation d'apprenants par un CSV), des livrets (ajout, modification, comptes-rendus, observations globales et suppression) et des informations relatives à ceux-ci (groupes, formateurs/matières, personnels, sites et entreprises) pour les administrateurs
- Gestion des modèles de livret d'apprentissage par rapport à un groupe et un site de formation et possibilité d'y ajouter des PDF "administratifs" comme un réglement intérieur par exemple pour le secteur qualité

# Installation

## Prérequis

- .NET 8 SDK 
- Visual Studio 2022 (avec le workload .NET MAUI installé)

## 1. Cloner le projet

```
cd /votre/projet/
git clone https://github.com/bastienchevalier5/CarnApprenti-Administration.git
```

## 2. Installer le certificat

Aller dans le dossier cloné :

```
CarnApprenti-Admninistration/CarnApprenti/CarnApprenti_1.0.0.0_Test
Puis double-cliquez sur CarnApprenti_1.0.0.0_x86.cer comme ci-dessous
```
![Capture d'écran 2025-03-13 213934](https://github.com/user-attachments/assets/e4b2cd50-d9fc-4ac8-b048-2016ea3f133d)

```
Cliquez sur Installer un certificat
```
![Capture d'écran 2025-03-13 214256](https://github.com/user-attachments/assets/0452a119-0afe-405c-a932-60816af1e900)

```
Puis choisissez Ordinateur local puis Suivant
```
![Capture d'écran 2025-03-13 214352](https://github.com/user-attachments/assets/21128b74-e5a6-47d6-9ad5-f5876af37e77)

```
Puis choisissez Placer tous les certificats dans le magasin suivant puis dans Parcourir choisissez Autorités de certification racines de confiance puis Suivant
```
![Capture d'écran 2025-03-13 214633](https://github.com/user-attachments/assets/2fa81969-78a4-47d3-9428-ea111b4d7f3d)

```
Ensuite vous avez une confirmation de ce que vous venez de faire, vous pouvez cliquer sur Terminer
```
![Capture d'écran 2025-03-13 214842](https://github.com/user-attachments/assets/79f027ea-e9c8-4230-baf6-84e6419acf6a)

## 3. Installer l'application

```
Maintenant, toujours dans le même dossier que le certificat vous pouvez ouvrir le fichier CarnApprenti_1.0.0.0_x86.msix
Une fenêtre s'ouvrira et vous pourrez cliquer sur Installer
```
