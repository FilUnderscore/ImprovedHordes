pipeline
{
    agent any
    
    parameters {
        string(name: 'version', defaultValue: params.version ? params.version : '2.0.0')
        string(name: 'prerelease_version', defaultValue: params.prerelease_version ? params.prerelease_version : '')
    }
    options {
        buildDiscarder(logRotator(numToKeepStr: '100', artifactNumToKeepStr: '100'))
    }
    stages {
        stage ('Setup Build Environment') {
            steps {
                sh "sudo sh setup-dev-linux.sh"
            }
        }
        stage ('Build Release') {
            steps {
                script {
                    BUILD_CONFIGURATION = env.BRANCH_NAME == 'stable' ? "Release" : "Experimental"
                }

                sh "sudo msbuild ./Source/ImprovedHordes/ImprovedHordes.csproj /p:Configuration=${BUILD_CONFIGURATION}"
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

                OLD_MODINFO_VERSION = sh (
                    script: "xmlstarlet sel -t -v '/ModInfo/Version/@value' ImprovedHordes/ModInfo.xml",
                    returnStdout: true
                ).trim()

                OLD_MANIFEST_VERSION = sh (
                    script: "xmlstarlet sel -t -v '/ModManifest/Version/text()' ImprovedHordes/Manifest.xml",
                    returnStdout: true
                ).trim()

                MODINFO_VERSION = params.version
                MANIFEST_VERSION = params.version + ((params.prerelease_version == null || params.prerelease_version.allWhitespace) ? '' : ('-' + params.prerelease_version))

                withCredentials([usernamePassword(credentialsId: "${env.CREDENTIALS}", usernameVariable: 'USER', passwordVariable: 'PASSWORD')]) {
                    sh "git config --global user.email '${env.CREDENTIALS_EMAIL}'"
                    sh "git config --global user.name \$USER"
                    
                    sh "git checkout -b ${env.BRANCH_NAME}"
                    sh "git pull"

                    if(env.BRANCH_NAME == 'dev') {
                        if(OLD_MODINFO_VERSION != MODINFO_VERSION || OLD_MANIFEST_VERSION != MANIFEST_VERSION) {
                            sh "sudo xmlstarlet edit --inplace --update '/ModInfo/Version/@value' --value '${MODINFO_VERSION}' ImprovedHordes/ModInfo.xml"
                            sh "sudo xmlstarlet edit --inplace --update '/ModManifest/Version' --value '${MANIFEST_VERSION}' ImprovedHordes/Manifest.xml"

                            sh "git add ImprovedHordes/ModInfo.xml ImprovedHordes/Manifest.xml"
                            sh "git commit -m 'Updated version to ${MANIFEST_VERSION}'."
                        }

                        try {                    
                            UPDATED_GAME_VERSION = sh (
                                script: "mono ../../VersionRelease.exe Dependencies/7DaysToDieServer_Data/Managed/Assembly-CSharp.dll ImprovedHordes/Manifest.xml",
                                returnStdout: true
                            ).trim()

                            sh "git add ImprovedHordes/Manifest.xml"
                            sh "git commit -m '${UPDATED_GAME_VERSION}'"
                        } catch (err) {

                        }
                    }

                    sh "git push https://\$USER:\$PASSWORD@github.com/FilUnderscore/ImprovedHordes.git ${env.BRANCH_NAME}"
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