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
                // Update build number in ModInfo file.
                def slurper = new groovy.util.XmlSlurper().parseText(xmlOriginal)
                def currentVersion = slurper.ModInfo.Version.@value
                def buildNo = currentBuild.number
                def split = currentVersion.split('\\.')
                def newVersion = ""

                for(int i = split.length; i < 3; i++) {
                    newVersion += split[i] + "."
                }
                
                newVersion += buildNo

                slurper.ModInfo.Version.@value = "2.0.0" + currentBuild.number
                def xmlModified = groovy.xml.XmlUtil.serialize(slurper)
                new File("ImprovedHordes/ModInfo.xml") << xmlModified
            }

            sh "mv ImprovedHordes ImprovedHordes-temp"
            sh "mkdir ImprovedHordes"
            sh "mv ImprovedHordes-temp ImprovedHordes/ImprovedHordes"
            zip zipFile: 'ImprovedHordes.zip', archive: false, dir: 'ImprovedHordes'
            archiveArtifacts artifacts: 'ImprovedHordes.zip', onlyIfSuccessful: true
        }
    }
}