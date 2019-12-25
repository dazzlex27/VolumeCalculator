from requests.auth import HTTPBasicAuth
import requests

result = requests.get('http://sha.zappstore.pro/package/tools/packup-calculate?wt=10&w=1&h=2&l=7&bar=12345', 
auth=HTTPBasicAuth('is', 'AKVc8ceDwUpu83ZRPp5EcVUC8GesWHgC'))
print(result.text)