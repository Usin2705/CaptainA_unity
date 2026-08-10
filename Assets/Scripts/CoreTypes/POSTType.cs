public enum POSTType
{
    MDD_TASK,
    LLM_TASK,
    PuheNumero_TASK,
    ASA_CONSENT,
    ASA_TASK,
    USER_ASA_FEEDBACK,
    ASA_PROFILE,

    // DELETE /users - the user asking for their data to be erased. Sent with the guid as
    // a form field and SERVER_DELETE_KEY in the X-Delete-Key header.
    DATA_DEL_REQUEST,
    OTHER,
}
