import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FileService {

  private url = 'http://localhost:5000/api/file/'

  constructor(private _http: HttpClient) { }

  getFiles(options: any) {
    return this._http.post<any>(this.url + 'files', options)
  }

  deleteFiles(fileIds: any) {
    const options = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      }),
      body: fileIds 
    }
    return this._http.delete<any>(this.url, options)
  }

  updateFile(file: any) {
    return this._http.put<any>(this.url, file)
  }

  downloadFile(fileId: string): Observable<any> {
    return this._http.get(this.url + fileId, {
      responseType: "blob"
    });
  }
}
