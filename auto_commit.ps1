# Chemin du répertoire à surveiller (modifie si nécessaire)
$path = "C:\Users\user.CARNAPPRENTI\source\repos\bastienchevalier5\CarnApprenti-Administration"

# Intervalle de vérification en secondes
$interval = 5 

# Message de commit par défaut
$commitMessage = "Auto-commit: modification détectée"

# Fonction pour vérifier les changements et pousser
function Watch-GitRepo {
    while ($true) {
        # Aller dans le répertoire du projet
        Set-Location $path

        # Vérifier si des fichiers ont changé
        $changes = git status --porcelain
        if ($changes) {
            Write-Host "Changements détectés, push en cours..."
            
            # Ajouter les fichiers, committer et pousser
            git add .
            git commit -m $commitMessage
            git push origin master # Remplace "main" par ta branche si nécessaire

            Write-Host "Push effectué avec succès !"
        }

        # Attendre avant la prochaine vérification
        Start-Sleep -Seconds $interval
    }
}

# Exécuter la surveillance
Watch-GitRepo
