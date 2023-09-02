pipeline
{
    agent any
    
    stages {
        stage ('Checkout') {
            steps {
                deleteDir()
                git url: 'https://github.com/FilUnderscore/ImprovedHordes.git', branch: env.BRANCH_NAME
            }
        }
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
                    script: "git rev-list --count ${env.BRANCH_NAME}",
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

                sh "sudo xmlstarlet edit --inplace --update '/ModInfo/Version/@value' --value '${MODINFO_VERSION}.${GIT_COMMIT_COUNT}' ImprovedHordes/ModInfo.xml"
                sh "sudo xmlstarlet edit --inplace --update '/ModManifest/Version' --value '${MANIFEST_VERSION}+${env.BRANCH_NAME}.${GIT_COMMIT_COUNT}.${GIT_COMMIT_HASH}' ImprovedHordes/Manifest.xml"
            }

            sh "mv ImprovedHordes ImprovedHordes-temp"
            sh "mkdir ImprovedHordes"
            sh "mv ImprovedHordes-temp ImprovedHordes/ImprovedHordes"
            zip zipFile: 'ImprovedHordes.zip', archive: false, dir: 'ImprovedHordes'
            archiveArtifacts artifacts: 'ImprovedHordes.zip', onlyIfSuccessful: true

            buildName "${MANIFEST_VERSION}+${GIT_COMMIT_COUNT}.${GIT_COMMIT_HASH}"
        }
    }
}