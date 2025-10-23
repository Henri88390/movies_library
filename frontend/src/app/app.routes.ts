import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { MoviesComponent } from './components/movies/movies.component';
import { RegisterComponent } from './components/register/register.component';

export const routes: Routes = [
  { path: '', redirectTo: '/movies', pathMatch: 'full' },
  {
    path: 'movies',
    component: MoviesComponent,
  },
  {
    path: 'login',
    component: LoginComponent,
  },
  {
    path: 'register',
    component: RegisterComponent,
  },
  { path: '**', redirectTo: '/movies' },
];
