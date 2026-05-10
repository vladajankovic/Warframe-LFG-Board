// This file ensures TypeScript recognizes CSS imports
declare module '*.css' {
  const classes: { [key: string]: string };
  export default classes;
}