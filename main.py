from flask import Flask, request, jsonify, Response
import requests
from flask_cors import CORS


app = Flask(__name__)

CORS(app)

# This is the URL of the insecure server you want to forward requests to
base_url = "http://{0}.pages.konpeki.co.uk"

@app.route('/<path:url>', methods=['GET', 'POST', 'PUT', 'DELETE'])
def proxy(url):
    # Split the URL into parts
    parts = url.split('/', 1)  # Split only on first slash to preserve full path
    if not parts:
        return jsonify({'error': 'Invalid URL format'}), 400
    
    username = parts[0]
    path = parts[1] if len(parts) > 1 else ''
    
    # Construct target URL preserving full path
    target_url = f"{base_url.format(username)}/{path}"
    if request.query_string:
        target_url = f"{target_url}?{request.query_string.decode('utf-8')}"

    print(f"Proxying to: {target_url}")
    
    # Forward headers but remove problematic ones
    headers = {k: v for k, v in request.headers.items()
              if k.lower() not in ['host', 'content-length']}
    headers['Host'] = f"{username}.pages.konpeki.co.uk"
    
    try:
        response = requests.request(
            method=request.method,
            url=target_url,
            headers=headers,
            data=request.get_data(),
            cookies=request.cookies,
        )
        
        # Filter response headers
        excluded_headers = ['content-encoding', 'content-length', 'transfer-encoding', 'connection']
        headers = [(name, value) for (name, value) in response.headers.items()
                  if name.lower() not in excluded_headers]
        
        return Response(
            response.raw,
            response.status_code,
            headers,
            direct_passthrough=True
        )
    
    except requests.RequestException as e:
        print(f"Error proxying request: {e}")
        return jsonify({'error': 'Failed to proxy request'}), 500

if __name__ == '__main__':
    # Run the Flask app on HTTPS (need your SSL certificates here)
    app.run(host='0.0.0.0', port=13123)
