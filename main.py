import requests

url = "http://root.pages.konpeki.co.uk/work"

response = requests.get(url)
print(response.text)