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
                MODINFO_VERSION = sh (
                    script: "xmlstarlet sel -t -v '/ModInfo/Version/@value' ImprovedHordes/ModInfo.xml",
                    returnStdout: true
                ).trim()

                sh "sudo xmlstarlet edit --inplace --update '/ModInfo/Version/@value' --value '${MODINFO_VERSION}.1' ImprovedHordes/ModInfo.xml"
            }

            sh "mv ImprovedHordes ImprovedHordes-temp"
            sh "mkdir ImprovedHordes"
            sh "mv ImprovedHordes-temp ImprovedHordes/ImprovedHordes"
            zip zipFile: 'ImprovedHordes.zip', archive: false, dir: 'ImprovedHordes'
            archiveArtifacts artifacts: 'ImprovedHordes.zip', onlyIfSuccessful: true
        }
    }
}