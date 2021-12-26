# MariaDB Operator

Kubernetes operator for mariadb.  
The operator is currently in alpha and should be treated as such.  
The code itself is quite messy and only manually tested (tests are coming!).

## Whats

### What it does

The operator checks for Custom Resources named `MariaDB`, when one is found,
it will create a deployment (single instance at the moment) with the specs passed
in the CRD and if specified a persistent volume claim for the database data.  

On resource updates, it will replace the currently deployed mariadb instance
with a new one.

On resource removal, the deployment is removed.

_Observe: PVCs created are not currently removed._

### What it should do more

  * Create clusters with new `MariaDBCluster` CRD.
  * Create service for multi instance databases.
  * Create Service for single instance databases.
  * Multi operator instances with leader election.
  * Optional PVC cleanup.

### What it might do more

  * Create databases in existing resources with new `MariaDBDatabase` CRD.
  * Create users in existing resources with new `MariaDBUser` CRD.
  * Create backups with a sidecar and spec via new `MariaDBBackup` CRD.

## How

### Installation

Installing the operator is done by applying the files in `kube/`, the folder
contains the `MariaDB` CRD, cluster wide RBAC (which you may change to namespaced if wanted)
and the operator deployment.  
  
Currently, due to limitations of the SDK, the operator is a single instance operator,
deploying multiple cluster-wide operators or operators in the same namespace
(or cluster wide + namespaced) will make the operators fight about the ownership, which is bad.

```shell
kubectl apply -f https://raw.githubusercontent.com/jitesoft/mariadb-operator/master/kube/rbac-cluster.yml
kubectl apply -f https://raw.githubusercontent.com/jitesoft/mariadb-operator/master/kube/crd.yml
kubectl apply -f https://raw.githubusercontent.com/jitesoft/mariadb-operator/master/kube/deployment.yml
```

### Helm?

No helm deployment exist yet, in case the operator is received well, there might be!

### Usage

Create a new MariaDB resource:

```yaml
apiVersion: jitesoft.tech/v1alpha1
kind: MariaDB
metadata:
  name: test-mariadb
  namespace: default
spec:
  dbName: just-a-db
  dbUser: testuser
  dbPasswordSecretName: test-maria-secret
  dataVolumeClaim:
    size: 10Gi
    storageType: hcloud-volumes
```

Done!

## License and such

The code for the operator is released under the [MIT](LICENSE) license.  
