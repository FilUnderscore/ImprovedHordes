pipeline
{
    agent any
    
    stages {
        stage ('Build') {
            steps {
                deleteDir()
                git url: 'https://github.com/FilUnderscore/ImprovedHordes.git', branch: 'dev'
                sh "sudo sh setup-dev-linux.sh"
                sh "sudo msbuild ./Source/ImprovedHordes/ImprovedHordes.csproj /p:Configuration=Experimental"
            }
        }
    }
    post {
        always {
            sh "mv ImprovedHordes ImprovedHordes-temp"
            sh "mkdir ImprovedHordes"
            sh "mv ImprovedHordes-temp ImprovedHordes/ImprovedHordes"
            zip zipFile: 'ImprovedHordes.zip', archive: false, dir: 'ImprovedHordes'
            archiveArtifacts artifacts: 'ImprovedHordes.zip', onlyIfSuccessful: true
        }
    }
}