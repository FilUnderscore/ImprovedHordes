pipeline
{
    agent any
    
    stages {
        stage ('Setup Build Environment') {
            steps {
                sh "sudo sh setup-dev-linux.sh"
            }
        }
        stage ('Build Release') {
            steps {
                sh "sudo msbuild ./Source/ImprovedHordes/ImprovedHordes.csproj /p:Configuration=Experimental"
            }
        }
    }
    post {
        success {
            script {
                GIT_COMMIT_HASH = sh (
                    script: "sudo git log -n 1 --pretty=format:'%h'",
                    returnStdout: true
                ).trim()

                GIT_COMMIT_COUNT = sh (
                    script: "git rev-list --count HEAD",
                    returnStdout: true
                ).trim()

                MODINFO_VERSION = sh (
                    script: "xmlstarlet sel -t -v '/ModInfo/Version/@value' ImprovedHordes/ModInfo.xml",
                    returnStdout: true
                ).trim()

                MANIFEST_VERSION = sh (
                    script: "xmlstarlet sel -t -v '/ModManifest/Version/text()' ImprovedHordes/Manifest.xml",
                    returnStdout: true
                ).trim()

                try {                    
                    UPDATED_GAME_VERSION = sh (
                        script: "mono ../../VersionRelease.exe Dependencies/7DaysToDieServer_Data/Managed/Assembly-CSharp.dll ImprovedHordes/Manifest.xml",
                        returnStdout: true
                    ).trim()

                    withCredentials([usernamePassword(credentialsId: "${env.CREDENTIALS}", usernameVariable: 'USER', passwordVariable: 'PASSWORD')]) {                
                        sh "git config --global user.email '${env.CREDENTIALS_EMAIL}'"
                        sh "git config --global user.name \$USER"

                        sh "git checkout -b ${env.BRANCH_NAME}"
                        sh "git pull"
                        sh "git add ImprovedHordes/Manifest.xml"
                        sh "git commit -m '${UPDATED_GAME_VERSION}'"
                        sh "git show-ref"
                        sh "git push https://\$USER:\$PASSWORD@github.com/FilUnderscore/ImprovedHordes.git ${env.BRANCH_NAME}"
                    }
                } catch (err) {

                }

                sh "sudo xmlstarlet edit --inplace --update '/ModInfo/Version/@value' --value '${MODINFO_VERSION}.${GIT_COMMIT_COUNT}' ImprovedHordes/ModInfo.xml"
                sh "sudo xmlstarlet edit --inplace --update '/ModManifest/Version' --value '${MANIFEST_VERSION}+${env.BRANCH_NAME}.${GIT_COMMIT_COUNT}.${GIT_COMMIT_HASH}' ImprovedHordes/Manifest.xml"
            }

            sh "rm ImprovedHordes/Config/nav_objects.xml"
            sh "mv ImprovedHordes ImprovedHordes-temp"
            sh "mkdir ImprovedHordes"
            sh "mv ImprovedHordes-temp ImprovedHordes/ImprovedHordes"
            zip zipFile: 'ImprovedHordes.zip', archive: false, dir: 'ImprovedHordes'
            archiveArtifacts artifacts: 'ImprovedHordes.zip', onlyIfSuccessful: true, fingerprint: true

            buildName "${MANIFEST_VERSION}+${GIT_COMMIT_COUNT}.${GIT_COMMIT_HASH}"
        }

        cleanup {
            deleteDir()
            
            // Delete tmp dir
            dir("${workspace}@tmp") {
                deleteDir()
            }
        }
    }
}